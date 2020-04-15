Notepad++ Menu Search Plugin 
----------------------------

![Screenshot](docs/screenshot.png)

This plugin adds a text field to the toolbar for searching menu items and preference dialog options.

The plugin is inspired by similar Visual Studio 2012 functionality. It is written in C# using the approach described in [https://sourceforge.net/projects/notepad-plus/forums/forum/482781/topic/4404278](https://sourceforge.net/projects/notepad-plus/forums/forum/482781/topic/4404278).

This plugin is available under the *GNU Library General Public License (LGPL 2.0)*, see [LICENSE.md](LICENSE.md).

Installation
------------

**Binaries are available** on [Sourceforge](https://sourceforge.net/projects/nppmenusearch/files/).  
Put `NppMenuSearch.dll` into a new `plugins\NppMenuSearch\` subfolder in the Notepad++ installation folder.

Usage
-----

The plugin adds a text box to the Notepad++ toolbar. 

* Use Ctrl+M or the pointing device to select it.
* Type some characters, e.g. "number con".
    As you type, a drop-down list of found menu items appears.
* UP and DOWN keys can be used to navigate through the drop-down list
* TAB switches between the menu items list and the preference dialog items list.
* ESC abandons the search.
* ENTER executes the currently selected menu item and closes the drop-down list.
    If a preferences dialog item was selected, the preferences dialog with that item highlighted.
* There is a menu item to repeat the most recently used command. You can assign a keyboard 
    shortcut to this if you need it often. This would effectively save you two keystrokes.
* The search results window has a context menu for changing keyboard shortcuts. 
