from PIL import Image
import os

# Set your folders here
input_folder = "ImagesToProcess"
output_folder = "zzRandomAffixesMod/UIAtlases/ItemIconAtlas"

# Create output folder if it doesn't exist
os.makedirs(output_folder, exist_ok=True)

# Supported image formats
supported_extensions = (".png", ".jpg", ".jpeg", ".bmp", ".gif", ".webp")

for filename in os.listdir(input_folder):
    if filename.lower().endswith(supported_extensions):
        input_path = os.path.join(input_folder, filename)
        output_path = os.path.join(output_folder, filename)

        with Image.open(input_path) as img:
            width, height = img.size

            # Resize only if larger than 160x160
            if width > 160 or height > 160:
                img = img.resize((160, 160), Image.LANCZOS)

            # Save image
            img.save(output_path)

print("Done! All images processed.")