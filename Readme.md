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
- Funktions-Attribute: ScreenUpdate, Thread, Debug, LiveDebug (noch in Entwicklung)
- Feld-Attribute: Debug


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
local testvar = 0

[LiveDebug]
function myFuncToDebug()
testvar = testvar + 1
platform.window:invalidate()
testvar = testvar + 1
platform.window:invalidate()
end

function on.paint(gc --[[Grafikgerät]])
myFuncToDebug()
gc:drawString(tostring(testvar), 50, 50, "top")
end
```
im Hintergrund generiert zu
```lua
local testvar = 0
local __liveDebug_enterPressed_myFuncToDebug_liveDebug964698839 = false
local __errorHandleVar1494363505 = ""
function OnFieldCall1494363505_Fail(err)
__errorHandleVar1494363505 = tostring(err)
end
function OnFieldCall1494363505()
testvar = testvar + 1
platform.window:invalidate()
end
local __errorHandleVar1089847783 = ""
function OnFieldCall1089847783_Fail(err)
__errorHandleVar1089847783 = tostring(err)
end
function OnFieldCall1089847783()
testvar = testvar + 1
platform.window:invalidate()
end
local myFuncToDebug_liveDebug964698839 = coroutine.create(function ()
coroutine.yield()
xpcall( OnFieldCall1494363505, OnFieldCall1494363505_Fail )
coroutine.yield()
xpcall( OnFieldCall1089847783, OnFieldCall1089847783_Fail )
end)
local __errorHandleVar1868329146 = ""
function OnFieldCall1868329146_Fail(err)
__errorHandleVar1868329146 = tostring(err)
end
function OnFieldCall1868329146()
testvar = testvar + 1
platform.window:invalidate()
end
local __errorHandleVar1919796017 = ""
function OnFieldCall1919796017_Fail(err)
__errorHandleVar1919796017 = tostring(err)
end
function OnFieldCall1919796017()
testvar = testvar + 1
platform.window:invalidate()
end
function ResumeFunc_myFuncToDebug_liveDebug964698839()
if coroutine.status(myFuncToDebug_liveDebug964698839) == "dead" then
myFuncToDebug_liveDebug964698839 = coroutine.create(function ()
coroutine.yield()
xpcall( OnFieldCall1868329146, OnFieldCall1868329146_Fail )
coroutine.yield()
xpcall( OnFieldCall1919796017, OnFieldCall1919796017_Fail )
end)
end
if not coroutine.running(myFuncToDebug_liveDebug964698839) and __liveDebug_enterPressed_myFuncToDebug_liveDebug964698839 then
coroutine.resume(myFuncToDebug_liveDebug964698839)
__liveDebug_enterPressed_myFuncToDebug_liveDebug964698839 = false
end
end
function myFuncToDebug()
ResumeFunc_myFuncToDebug_liveDebug964698839()
end
function onpaint(gc --[[Grafikgerät]])
gc:drawString("[DebugMode] "..__errorHandleVar1919796017, 150, 52, "top")
gc:drawString("[DebugMode] "..__errorHandleVar1868329146, 150, 39, "top")
gc:drawString("[DebugMode] "..__errorHandleVar1089847783, 150, 26, "top")
gc:drawString("[DebugMode] "..__errorHandleVar1494363505, 150, 13, "top")
myFuncToDebug()
gc:drawString(tostring(testvar), 50, 50, "top")
end
function ontabKey()
__liveDebug_enterPressed_myFuncToDebug_liveDebug964698839 = true
ResumeFunc_myFuncToDebug_liveDebug964698839()
end
```
Fehlend: Anzeige für die aktuelle Code-Position

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
- Um das GUI in dem DevelopmentMode zu bringen, muss eine LunaGUI.debug-Datei im selben Verzeichnis existieren.
- Beim LiveDebugging müssen alle Funktionen vor der ontabKey-Funktion deklariert sein, falls eine ontabKey-Funktion vorhanden ist.
- Beim Debug-Attribut werden ScreenUpdate-Befehle (platform.window.invalidate()) hinter dem Feld mitübernommen.

##Setup
Systemanforderungen:
.Net 4.0 Framework oder neuer

Download (nur .rar):
[Windows XP/Vista/7/8/10](https://github.com/DanThePman/Luna-GUI/blob/master/Luna%20GUI/bin/x86/)