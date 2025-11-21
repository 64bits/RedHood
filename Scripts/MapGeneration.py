import csv
from PIL import Image, ImageDraw

# Configuration
TEXTURE_SIZE = 2048
TREE_WIDTH = 20
TREE_HEIGHT = 80
CORNER_RADIUS = 10

# Color mapping for tree types 1-5
TREE_COLORS = {
    1: (73, 73, 81),    # Forest Green
    2: (85, 83, 94),   # Olive Drab
    3: (204, 78, 0),    # Dark Orange
    4: (255, 139, 30),    # Light Orange
    5: (44, 111, 33),   # Dark Green
}

def get_tree_type(name):
    """Extract tree type number from name (e.g., 'T3' -> 3, 'T4.001' -> 4)"""
    # Remove 'T' prefix and get the integer part
    num_str = name[1:].split('.')[0]
    return int(num_str)

def draw_rounded_rect(draw, x, y, width, height, radius, color):
    """Draw a rounded rectangle centered at (x, y)"""
    left = x - width // 2
    top = y - height // 2
    right = x + width // 2
    bottom = y + height // 2
    
    # Draw the rounded rectangle using pieslices and rectangles
    draw.rectangle([left + radius, top, right - radius, bottom], fill=color)
    draw.rectangle([left, top + radius, right, bottom - radius], fill=color)
    draw.pieslice([left, top, left + 2*radius, top + 2*radius], 180, 270, fill=color)
    draw.pieslice([right - 2*radius, top, right, top + 2*radius], 270, 360, fill=color)
    draw.pieslice([left, bottom - 2*radius, left + 2*radius, bottom], 90, 180, fill=color)
    draw.pieslice([right - 2*radius, bottom - 2*radius, right, bottom], 0, 90, fill=color)

def load_trees(csv_path):
    """Load tree data from CSV file"""
    trees = []
    with open(csv_path, 'r') as f:
        reader = csv.DictReader(f)
        for row in reader:
            trees.append({
                'name': row['Name'],
                'x': float(row['X']),
                'y': float(row['Y']),
                'type': get_tree_type(row['Name'])
            })
    return trees

def normalize_coordinates(trees, texture_size):
    """Normalize coordinates to fit within texture, with padding"""
    if not trees:
        return trees
    
    xs = [t['x'] for t in trees]
    ys = [t['y'] for t in trees]
    
    min_x, max_x = min(xs), max(xs)
    min_y, max_y = min(ys), max(ys)
    
    # Add padding
    padding = 100
    usable_size = texture_size - 2 * padding
    
    range_x = max_x - min_x if max_x != min_x else 1
    range_y = max_y - min_y if max_y != min_y else 1
    
    for tree in trees:
        tree['norm_x'] = int(padding + (tree['x'] - min_x) / range_x * usable_size)
        tree['norm_y'] = int(padding + (tree['y'] - min_y) / range_y * usable_size)
        # Store normalized Y for depth (0 to 1)
        tree['depth'] = (tree['y'] - min_y) / range_y
    
    return trees

def create_color_texture(trees, size):
    """Create the color texture with trees colored by type"""
    img = Image.new('RGBA', (size, size), (0, 0, 0, 0))
    draw = ImageDraw.Draw(img)
    
    # Sort by Y position so back rows (lower Y) are drawn first
    sorted_trees = sorted(trees, key=lambda t: t['norm_y'])
    
    for tree in sorted_trees:
        color = TREE_COLORS.get(tree['type'], (128, 128, 128))
        draw_rounded_rect(draw, tree['norm_x'], tree['norm_y'], 
                         TREE_WIDTH, TREE_HEIGHT, CORNER_RADIUS, color)
    
    return img

def create_depth_texture(trees, size):
    """Create the depth texture with trees colored by Y position"""
    img = Image.new('RGBA', (size, size), (0, 0, 0, 0))
    draw = ImageDraw.Draw(img)
    
    # Sort by Y position so back rows (lower Y) are drawn first
    sorted_trees = sorted(trees, key=lambda t: t['norm_y'])
    
    for tree in sorted_trees:
        # Depth value from 0 (black) to 255 (white) based on Y position
        depth_value = int(tree['depth'] * 255)
        color = (depth_value, depth_value, depth_value)
        draw_rounded_rect(draw, tree['norm_x'], tree['norm_y'],
                         TREE_WIDTH, TREE_HEIGHT, CORNER_RADIUS, color)
    
    return img

def main():
    # Load and process trees
    trees = load_trees('map.csv')
    trees = normalize_coordinates(trees, TEXTURE_SIZE)
    
    # Create textures
    color_texture = create_color_texture(trees, TEXTURE_SIZE)
    depth_texture = create_depth_texture(trees, TEXTURE_SIZE)
    
    # Save textures
    color_texture.save('trees_color.png')
    depth_texture.save('trees_depth.png')
    
    print(f"Generated textures for {len(trees)} trees")
    print("Saved: trees_color.png, trees_depth.png")

if __name__ == '__main__':
    main()