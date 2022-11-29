#!/bin/bash
ffmpeg -i $1 -i $2 -filter_complex join=inputs=2:channel_layout=stereo $3