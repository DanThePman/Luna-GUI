<img src="icon.png" align="right" />
# Luna GUI
Das Luna GUI sorgt für die Aktualisierung von Code-Snippets innerhalb der Sublime-Text-Entwicklungsumgebung, um effizienter Code 
fuer den Taschenrechner zu erstellen und besitzt einen integrierten Compiler, der das Erstellen und übertragen von tns-Dateien 
ermöglicht.
Da das Drag&Drop-Verfahren simuliert wird, müssen Positionen für Buttons und Felder festgelegt werden.
Ein OffsetFinder-Wizard im Tab "Testing" hilft beim Ermitteln der relativen Fenster- und Buttonpositionen, die gesetzt werden müssen,
um Dateien automatisch auf den Emulator übertragen zu können.

Hinweise:
- Reload-Button => Gelber Button, der im Dateiuebertragungsfenster steht
- Zielordner-Textfeld => unter Einstellungen->Dateiübertragung das Textfeld neben "Ziel-Ordner"
- Taskleistensymbol => von Firebird

## CodeAnalyse
Aktuelle Funktionen:
- MoonSharp Lua-Compiler
- Eventkonvertierung
- Deklarationsüberprüfung
- Funktions-Attribute: ScreenUpdate, Debug, Thread
- Feld-Attribute: Debug

Zufünftige:
- Live-Debugging (eventuell)


##Attribute
###Debug
```lua
function on.paint( gc )
Funktionsname()
end

[Debug]
function Funktionsname()
a = 6
tostring(a)
end
```
im Hintergrund generiert zu
```lua
function onpaint( gc )
gc:drawString("[DebugActive] "..__errorHandleVar788398410, 150, 5, "top")
Funktionsname()
end
function Funktionsname788398410 ()
a = 6
tostring(a)
end
local __errorHandleVar788398410 = ""
function onFunction788398410_Fail(err)
__errorHandleVar788398410 = tostring(err)
end
function Funktionsname()
xpcall( Funktionsname788398410, onFunction788398410_Fail )
end
```
###Thread
```lua
[Thread]
function funcccccccccccc()
a = a+1
if a > 50 then a = 0 end
platform.window:invalidate()
return a
end
```
im Hintergrund generiert zu
```lua
function ThreadCloneFunc_funcccccccccccc()
local funcccccccccccc1458243235 = coroutine.wrap(function ()
a = a+1
if a > 50 then a = 0 end
platform.window:invalidate()
coroutine.yield(a)
end)
return funcccccccccccc1458243235()
end
function funcccccccccccc()
return ThreadCloneFunc_funcccccccccccc()
end
```
###LiveDebug [Alpha]
![Image](https://raw.githubusercontent.com/DanThePman/Luna-GUI/master/liveDebugExplanation.png)
```lua
function on.paint(gc --[[Grafikgerät]])

end

[LiveDebug]
function myFuncToDebug()
local b = 0
tostring(b)
end
```
im Hintergrund generiert zu
```lua
local __liveDebug_enterPressed_myFuncToDebug_liveDebug1433282804 = false
local __errorHandleVar1671899066 = ""
function OnFieldCall1671899066_Fail(err)
__errorHandleVar1671899066 = tostring(err)
end
function OnFieldCall1671899066()
local b = 0
end
local __errorHandleVar826353691 = ""
function OnFieldCall826353691_Fail(err)
__errorHandleVar826353691 = tostring(err)
end
function OnFieldCall826353691()
tostring(b)
end
local myFuncToDebug_liveDebug1433282804 = coroutine.create(function ()
corountine.yield()
xpcall( OnFieldCall1671899066, OnFieldCall1671899066_Fail )
corountine.yield()
xpcall( OnFieldCall826353691, OnFieldCall826353691_Fail )
end)
local __errorHandleVar718892105 = ""
function OnFieldCall718892105_Fail(err)
__errorHandleVar718892105 = tostring(err)
end
function OnFieldCall718892105()
local b = 0
end
local __errorHandleVar1856393609 = ""
function OnFieldCall1856393609_Fail(err)
__errorHandleVar1856393609 = tostring(err)
end
function OnFieldCall1856393609()
tostring(b)
end
function ResumeFunc_myFuncToDebug_liveDebug1433282804()
if coroutine.status(myFuncToDebug_liveDebug1433282804) == "dead" then
myFuncToDebug_liveDebug1433282804 = coroutine.create(function ()
corountine.yield()
xpcall( OnFieldCall718892105, OnFieldCall718892105_Fail )
corountine.yield()
xpcall( OnFieldCall1856393609, OnFieldCall1856393609_Fail )
end)
end
if not coroutine.running(myFuncToDebug_liveDebug1433282804) and __liveDebug_enterPressed_myFuncToDebug_liveDebug1433282804 then
coroutine.resume(myFuncToDebug_liveDebug1433282804())
__liveDebug_enterPressed_myFuncToDebug_liveDebug1433282804 = false
end
end
function onpaint(gc --[[Grafikgerät]])
gc:drawString("[DebugMode] "..__errorHandleVar1856393609, 150, 20, "top")
gc:drawString("[DebugMode] "..__errorHandleVar718892105, 150, 15, "top")
gc:drawString("[DebugMode] "..__errorHandleVar826353691, 150, 10, "top")
gc:drawString("[DebugMode] "..__errorHandleVar1671899066, 150, 5, "top")
end
function myFuncToDebug()
ResumeFunc_myFuncToDebug_liveDebug1433282804()
end
function ontabKey()
__liveDebug_enterPressed_myFuncToDebug_liveDebug1433282804 = true
ResumeFunc_myFuncToDebug_liveDebug1433282804()
end

```

##Screenshots
![Image](https://raw.githubusercontent.com/DanThePman/Luna-GUI/master/luaGuiMenu.png)
![Image](https://raw.githubusercontent.com/DanThePman/Luna-GUI/master/lunaGuiCodeChanges.png)
![Image](https://raw.githubusercontent.com/DanThePman/Luna-GUI/master/lunaGuiTesting.png)


##Fehler bei Windows XP
Unable to cast COM object of type System.__ComObject to interface type X 
- => Lösung:
- regsvr32 "C:\Program Files\Internet Explorer\ieproxy.dll"

HttpWebrequests können nicht zu Github aufgebaut werden.
- => Keine Codesnippet-Updates

##Hinweise
Um das GUI in dem DevelopmentMode zu bringen, muss eine LunaGUI.debug-Datei im selben Verzeichnis existieren.

##Setup
Systemanforderungen:
.Net 4.0 Framework oder neuer

Download (nur .rar):
[Windows XP/Vista/7/8/10](https://github.com/DanThePman/Luna-GUI/blob/master/Luna%20GUI/bin/x86/)