Notepad++ Menu Search Plugin 
----------------------------

![Screenshot](docs/screenshot.png)

This plugin adds a text field to the toolbar for searching menu items and preference dialog options.

The plugin is inspired by a similar Visual Studio 2012 functionality. It is written in C# using the approach described in [http://sourceforge.net/projects/notepad-plus/forums/forum/482781/topic/4404278](http://sourceforge.net/projects/notepad-plus/forums/forum/482781/topic/4404278).

This plugin is available under the *GNU Library General Public License (LGPL 2.0)*, see [LICENSE.md](LICENSE.md).

Installation
------------

Binaries are available on [Sourceforge](https://sourceforge.net/projects/nppmenusearch/files/).
Put `NppMenuSearch.dll` into the plugins subfolder of the Notepad++ installation folder as `plugins\NppMenuSearch\NppMenuSearch.dll`.

Usage
-----

The plugin adds a text box to the Notepad++ toolbar. 

* Use Ctrl+F1 or the mouse to select it.
* Type some characters, e.g. "number con".
    As you type, a drop-down list of found menu items appears.
* UP and DOWN keys can be used to navigate through the drop-down list
* TAB switches between the menu items list, the list of open file tabs, and the preference dialog items list.
* ESC abandons the search.
* ENTER executes the currently selected menu item and closes the drop-down list.
    If a preferences dialog item was selected, the preferences dialog with that item gets highlighted.
* There is a menu item to repeat the most recently used command. You should assign a keyboard 
    shortcut to this if you need it often. This would effectively save you two keystrokes.
* The search results window has a popup menu for changing shortcuts. 
