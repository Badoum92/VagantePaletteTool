Extract palettes from images.
The output formats are:
	- RGBA json like array
	- Hex code list
	- png palette

Put all of your images (preferably png) in the input directory.
If you want a set of images to count as one palette put them in their separate folder.
1 line images present in the input directory (not inside a subdirectory) will output a not sorted palette.

Examples
----------

input/wisp.png

Generates

output/wisp/wisp.txt
output/wisp/wisp_hex.txt
output/wisp/wisp.png

(output not sorted if wisp.png is only 1 pixel high)

----------

input/dragon/dragon_fly.png
input/dragon/dragon_sleep.png
input/dragon/dragon_dead.png

Generates

output/dragon.txt
output/dragon_hex.txt
output/dragon.png

(will always be sorted because inside a subdirectory)