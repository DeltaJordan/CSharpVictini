import markovify
import os

script_dir = os.path.dirname(__file__)

with open(os.path.join(script_dir, "fileLoc.txt"), 'rt', encoding='utf-8') as e:
	fileLoc = e.read()

with open(os.path.join(script_dir, fileLoc), 'rt', encoding='utf-8', errors='ignore') as f:
    text = f.read()

text_model = markovify.Text(text)

with open(os.path.join(script_dir, "output.txt"), "w+", encoding='utf-8', errors='ignore') as g:
	g.write(text_model.make_sentence(tries=100)) #test_output=False