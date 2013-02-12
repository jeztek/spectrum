#!/usr/bin/env python

# filespeed.py
# Rudimentary script to see how fast a file's size changes over time

import sys, os, time

if len(sys.argv) != 2:
    print "Usage:", sys.argv[0], "<filename>"
    sys.exit(-1)

filename = sys.argv[1]

prevsize = 0
while(True):
    size = os.path.getsize(filename)
    delta = size - prevsize
    print delta, "bytes/sec"
    prevsize = size
    time.sleep(1)
