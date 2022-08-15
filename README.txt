Extract palettes from images.
The output formats are:
	- RGBA json like array
	- Hex code list
	- png palette

Put all of your images (preferably png) in the input directory.
If you want a set of images to count as one palette put them in their separate folder.
1 line images present in the input directory (not inside a subdirectory) will output a not sorted palette.

You can also put .json files that include one palette that needs to have a similar format to the palettes.json which is contained in the game's files.

The game files can be extracted using this tool: https://github.com/mtolly/vagante-extract

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

----------

input/darkbat.json which has the following:

{
    "name": "dark",
    "colors": [
        [
            0,0,0,255
        ],[
            10,9,9,255
        ]
    ]
}

Generates

output/darkbat.txt
output/darkbat_hex.txt
output/darkbat.png