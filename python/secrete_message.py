def decode(message_file):
    
    with open(message_file, 'r') as file:
        lines = file.readlines()

    # Create a dictionary where the keys are the numbers and the values are the words
    num_word_dict = {int(line.split()[0]): line.split()[1] for line in lines}

    # Initialize the message and the current step of the pyramid
    message = ''
    step = 1

    # Iterate over the pyramid
    for i in range(1, len(num_word_dict) + 1):
        # If we're at the end of the current step of the pyramid
        if i == step * (step + 1) // 2:
            # Add the corresponding word to the message
            message += num_word_dict[i] + ' '
            # Move to the next step of the pyramid
            step += 1

    return message.rstrip()  # Remove the trailing space




print(decode('text.txt'))