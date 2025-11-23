import csv
import math
from PIL import Image, ImageDraw

# Configuration
TEXTURE_SIZE = 2048
TREE_WIDTH = 20
TREE_HEIGHT = 80
CORNER_RADIUS = 10
PADDING = 100

# Orientation Settings
# 0   = North is Top of image
# 90  = North is Right of image
# 180 = North is Bottom of image
# 270 = North is Left of image
NORTH_ANGLE = 90 

# Color mapping for tree types 1-5
TREE_COLORS = {
    1: (73, 73, 81),    # Forest Green
    2: (85, 83, 94),    # Olive Drab
    3: (204, 78, 0),    # Dark Orange
    4: (255, 139, 30),  # Light Orange
    5: (44, 111, 33),   # Dark Green
}

def get_tree_type(name):
    """Extract tree type number from name (e.g., 'T3' -> 3, 'T4.001' -> 4)"""
    try:
        # Remove 'T' prefix and get the integer part
        num_str = name.upper().replace('T', '').split('.')[0]
        return int(num_str)
    except ValueError:
        return 1 # Default to type 1 if parsing fails

def rotate_point(x, y, angle):
    """Rotates x, y around (0,0) by the specified angle (90 degree steps)"""
    if angle == 0:
        return x, y
    elif angle == 90:
        return y, -x
    elif angle == 180:
        return -x, -y
    elif angle == 270:
        return -y, x
    return x, y

def draw_rounded_rect(draw, x, y, width, height, radius, color):
    """Draw a rounded rectangle centered at (x, y)"""
    left = x - width // 2
    top = y - height // 2
    right = x + width // 2
    bottom = y + height // 2
    
    # Draw the rounded rectangle
    draw.rectangle([left + radius, top, right - radius, bottom], fill=color)
    draw.rectangle([left, top + radius, right, bottom - radius], fill=color)
    draw.pieslice([left, top, left + 2*radius, top + 2*radius], 180, 270, fill=color)
    draw.pieslice([right - 2*radius, top, right, top + 2*radius], 270, 360, fill=color)
    draw.pieslice([left, bottom - 2*radius, left + 2*radius, bottom], 90, 180, fill=color)
    draw.pieslice([right - 2*radius, bottom - 2*radius, right, bottom], 0, 90, fill=color)

def load_trees(csv_path):
    """Load tree data from CSV file"""
    trees = []
    try:
        with open(csv_path, 'r') as f:
            reader = csv.DictReader(f)
            for row in reader:
                trees.append({
                    'name': row['Name'],
                    'x': float(row['X']),
                    'y': float(row['Y']),
                    'type': get_tree_type(row['Name'])
                })
    except FileNotFoundError:
        print(f"Error: Could not find {csv_path}")
        return []
    return trees

def normalize_coordinates(trees, texture_size, angle):
    """Normalize coordinates to fit within texture, handling rotation and inversion"""
    if not trees:
        return trees
    
    usable_size = texture_size - 2 * PADDING

    # 1. Apply Rotation to raw coordinates
    # We modify the list in place with temporary rotated coordinates
    for tree in trees:
        rx, ry = rotate_point(tree['x'], tree['y'], angle)
        tree['rot_x'] = rx
        tree['rot_y'] = ry

    # 2. Find bounds of rotated coordinates
    xs = [t['rot_x'] for t in trees]
    ys = [t['rot_y'] for t in trees]
    
    min_x, max_x = min(xs), max(xs)
    min_y, max_y = min(ys), max(ys)
    
    range_x = max_x - min_x if max_x != min_x else 1
    range_y = max_y - min_y if max_y != min_y else 1
    
    # 3. Normalize to image space
    for tree in trees:
        # X Axis: Standard mapping (Left to Right)
        norm_x_ratio = (tree['rot_x'] - min_x) / range_x
        tree['norm_x'] = int(PADDING + norm_x_ratio * usable_size)
        
        # Y Axis: INVERTED mapping (Map Y-Up to Image Y-Down)
        # High Map Y (Top) should be Low Image Y (0)
        norm_y_ratio = (tree['rot_y'] - min_y) / range_y
        tree['norm_y'] = int(PADDING + (1.0 - norm_y_ratio) * usable_size)
        
        # Depth for texture (0 to 1)
        # Usually "Back" of image (Top, Low Pixel Y) is far (0)
        # "Front" of image (Bottom, High Pixel Y) is close (1)
        # Since norm_y is already inverted correctly for the image, 
        # we can use the pixel position ratio for depth.
        tree['depth'] = tree['norm_y'] / texture_size
    
    return trees

def create_color_texture(trees, size):
    img = Image.new('RGBA', (size, size), (0, 0, 0, 0))
    draw = ImageDraw.Draw(img)
    
    # Sort by norm_y (Pixel Y) ascending. 
    # Small Y (Top/Back) draws first. Large Y (Bottom/Front) draws last.
    sorted_trees = sorted(trees, key=lambda t: t['norm_y'])
    
    for tree in sorted_trees:
        color = TREE_COLORS.get(tree['type'], (128, 128, 128))
        draw_rounded_rect(draw, tree['norm_x'], tree['norm_y'], 
                         TREE_WIDTH, TREE_HEIGHT, CORNER_RADIUS, color)
    
    return img

def create_depth_texture(trees, size):
    img = Image.new('RGBA', (size, size), (0, 0, 0, 0))
    draw = ImageDraw.Draw(img)
    
    # Sort by norm_y so overlap is correct
    sorted_trees = sorted(trees, key=lambda t: t['norm_y'])
    
    for tree in sorted_trees:
        # Depth based on screen position (Back is dark, Front is light)
        # Multiply by 255 for grayscale
        depth_value = int(tree['depth'] * 255)
        
        # Clamp value just in case
        depth_value = max(0, min(255, depth_value))
        
        color = (depth_value, depth_value, depth_value)
        draw_rounded_rect(draw, tree['norm_x'], tree['norm_y'],
                         TREE_WIDTH, TREE_HEIGHT, CORNER_RADIUS, color)
    
    return img

def main():
    print(f"Processing with North oriented: {NORTH_ANGLE} degrees")
    
    trees = load_trees('map.csv')
    if not trees:
        return

    trees = normalize_coordinates(trees, TEXTURE_SIZE, NORTH_ANGLE)
    
    color_texture = create_color_texture(trees, TEXTURE_SIZE)
    depth_texture = create_depth_texture(trees, TEXTURE_SIZE)
    
    color_texture.save('trees_color.png')
    depth_texture.save('trees_depth.png')
    
    print(f"Generated textures for {len(trees)} trees")
    print("Saved: trees_color.png, trees_depth.png")

if __name__ == '__main__':
    main()