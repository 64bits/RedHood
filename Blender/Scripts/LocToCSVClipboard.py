import bpy
import csv
import io

# Get selected objects
selected_objects = bpy.context.selected_objects

# Build CSV text in memory
output = io.StringIO()
writer = csv.writer(output)
writer.writerow(["Name", "X", "Y"])  # Header

for obj in selected_objects:
    loc = obj.location
    writer.writerow([obj.name, loc.x, loc.y])

csv_text = output.getvalue()
output.close()

# Copy to clipboard
bpy.context.window_manager.clipboard = csv_text

print("CSV copied to clipboard!")
