#!/bin/bash
time dotnet run -c Release
magick -delay 6 -loop 0 frame_*.ppm orbit.gif
rm frame_*.ppm
