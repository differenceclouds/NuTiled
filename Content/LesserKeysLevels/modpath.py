import os
import xml.etree.ElementTree as ET

def update_tileset_sources(folder_path):
    # Loop through all files in the specified folder
    for filename in os.listdir(folder_path):
        # Check if the file has an XML extension
        if filename.endswith(".tmx"):
            file_path = os.path.join(folder_path, filename)
            
            # Parse the XML file
            tree = ET.parse(file_path)
            root = tree.getroot()
            modified = False  # Track if any changes are made

            # Iterate over all 'tileset' elements under the 'map' root
            for tileset in root.findall("tileset"):
                source = tileset.get("source")
                
                # Check if the source attribute starts with "../"
                if source and source.startswith("../"):
                    # Update the source attribute
                    new_source = source[3:]  # Remove the "../" prefix
                    tileset.set("source", new_source)
                    modified = True
                    print(f"Updated source in {filename}: {source} -> {new_source}")

            # If any changes were made, save the updated XML file
            if modified:
                tree.write(file_path, xml_declaration=True, encoding="UTF-8")
                print(f"Saved changes to {filename}")

# Example usage
print("it's open")
folder_path = "."
update_tileset_sources(folder_path)
