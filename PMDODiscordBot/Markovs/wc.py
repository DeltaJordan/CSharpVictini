#!/usr/bin/env python

from os import path
from wordcloud import WordCloud

d = path.dirname(__file__)

# Read the whole text.
text = open(path.join(d, 'wordcloudinput.txt'), 'rt', encoding='utf-8', errors='ignore').read()

# Generate a word cloud image
wordcloud =  WordCloud(relative_scaling=0.3, max_words=750, width=1280, height=720).generate(text)

wordcloud.to_file(path.join(d, "wc.png"))