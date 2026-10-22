# Third-party notices

ThinkTime's original source code and documentation are licensed under the MIT License. The materials listed below retain their respective source terms and attribution requirements and are not relicensed by ThinkTime.

## Natural Earth

ThinkTime includes the following Natural Earth vector datasets in `Assets/Geodata`:

- `ne_110m_land.geojson`
- `ne_110m_admin_0_countries.geojson`

Natural Earth data is in the public domain. Source and terms: https://www.naturalearthdata.com/about/terms-of-use/

The files were obtained from the `cothinking-dev/greyline` repository. Greyline itself is GPL-2.0-or-later; ThinkTime does not copy or link its program code. The astronomical and rendering implementations in ThinkTime are independently written in C# from published equations and the Natural Earth data.

## China administrative boundary overlay

`Assets/Geodata/china_full.geojson` is the nationwide province-level boundary dataset retrieved from Alibaba Cloud DataV GeoAtlas:

https://geo.datav.aliyun.com/areas_v3/bound/100000_full.json

This supplemental layer is used to replace Natural Earth's generalized boundary treatment in the China region. It is not represented as an official map-review artifact or as carrying a Chinese map approval number.

## NASA Earth at Night / Black Marble

`Assets/Textures/BlackMarble_2016_01deg_gray.jpg` is the 2016 global grayscale Earth-at-night composite distributed by NASA Earth Observatory. ThinkTime converts the black background to a transparent warm-light texture and reveals it only on the astronomical night side.

Source: https://science.nasa.gov/earth/earth-observatory/earth-at-night/maps/

Credit: NASA Earth Observatory images by Joshua Stevens, using Suomi NPP VIIRS data from Miguel Román, NASA Goddard Space Flight Center. NASA should be acknowledged as the source; its name and identifiers must not be used to imply endorsement.
