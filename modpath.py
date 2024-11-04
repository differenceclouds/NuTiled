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




import os
import xml.etree.ElementTree as ET

def update_sources(folder_path):
    # Loop through all files in the specified folder
    for filename in os.listdir(folder_path):
        # Check if the file has an XML extension
        if filename.endswith(".tmx"):
            file_path = os.path.join(folder_path, filename)
            
            # Parse the XML file
            tree = ET.parse(file_path)
            root = tree.getroot()
            modified = False  # Track if any changes are made

            # Update 'source' attributes in 'tileset' elements
            for tileset in root.findall("tileset"):
                source = tileset.get("source")
                
                if source and source.startswith("../"):
                    new_source = source[3:]  # Remove the "../" prefix
                    tileset.set("source", new_source)
                    modified = True
                    print(f"Updated source in {filename}: {source} -> {new_source}")

            # Update 'template' attributes in 'object' elements
            for obj in root.findall(".//object"):
                template = obj.get("template")
                
                if template and template.startswith("../"):
                    new_template = template[3:]  # Remove the "../" prefix
                    obj.set("template", new_template)
                    modified = True
                    print(f"Updated template in {filename}: {template} -> {new_template}")

            # If any changes were made, save the updated XML file with XML declaration
            if modified:
                tree.write(file_path, xml_declaration=True, encoding="UTF-8")
                print(f"Saved changes to {filename}")

# Example usage
folder_path = "path/to/your/xml/folder"
update_sources(folder_path)
