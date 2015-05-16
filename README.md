ImageResizer.Plugins.GradientOverlay
====================================

![fish example](https://raw.githubusercontent.com/Geta/ImageResizer.Plugins.GradientOverlay/master/content/demo.jpg)

(no actual fish harmed in the making of this picture, they are latex replicas)

### Parameters

* **gradient** - values '1' or 'true', specifies if plugin should be used.
* **gmode** - values 'r' for radial gradient or omitted for linear.
* **gstart** - values x or x,y (0 - 100)(percent of image width/height), start position of gradient (can be lower than 0 and higher than 100)(default 0,0 for l-gradient 50,50 for r-gradient).
* **gend** - values x or x,y (0 - 100), end position of gradient (default 0,100 for l-gradient 100,100 for r-gradient)
* **gcstart** - gradient start color, hexadecimal color value (argb shorthand f000 or full ff000000 are accepted)(default 0000).
* **gcend** - gradient end color, hexadecimal color value (argb shorthand f000 or full ff000000 are accepted)(default f000).
* **gclamp** - center clamping (useful for r-gradients), amount the center value should be pushed outwards (0-100 percent from start to end)(default 0).
