#!/bin/sh
# Render de inspeção com fundo escuro. uso: sh foto_sh.sh <yaw> <corpo|cabeca> <nome>
B="/c/Program Files/Blender Foundation/Blender 5.1/blender.exe"
W=$(pwd -W)
"$B" -b build/kai.blend --python foto.py -- "$W/build/$3.png" "$1" "$2" 2>&1 | grep -E "Error|Traceback|error:" | head
python compoe.py "$3"
