import os

def replace_spaces_in_filenames(folder_path):
    # Get all files in the directory
    for filename in os.listdir(folder_path):
        # Check if the file name contains spaces
        if ' ' in filename:
            # Create new filename by replacing spaces with underscores
            new_filename = filename.replace(' ', '_')
            # Get the full path for both old and new filenames
            old_file = os.path.join(folder_path, filename)
            new_file = os.path.join(folder_path, new_filename)
            # Rename the file
            os.rename(old_file, new_file)
            print(f'Renamed: {filename} -> {new_filename}')

# Example usage
folder_path = input("Enter the folder path: ")
if os.path.exists(folder_path):
    replace_spaces_in_filenames(folder_path)
    print("Process completed!")
else:
    print("The specified folder does not exist!")