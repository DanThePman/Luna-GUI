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
- Funktions-Attribute: ScreenUpdate, Thread, Debug (in Entwicklung), LiveDebug (in Entwicklung)
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
###LiveDebugging - mit StackTrace
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
local __liveDebug_enterPressed_myFuncToDebug_liveDebug1720878949 = false
local __liveDebug_currentCodePosition_myFuncToDebug_liveDebug1720878949 = "Not started"
local __errorHandleVar1600202380 = ""
function OnFieldCall1600202380_Fail(err)
__errorHandleVar1600202380 = tostring(err)
end
function OnFieldCall1600202380()
testvar = testvar + 1
platform.window:invalidate()
end
local __errorHandleVar1546312225 = ""
function OnFieldCall1546312225_Fail(err)
__errorHandleVar1546312225 = tostring(err)
end
function OnFieldCall1546312225()
testvar = testvar + 1
platform.window:invalidate()
end
local myFuncToDebug_liveDebug1720878949 = coroutine.create(function ()
__liveDebug_currentCodePosition_myFuncToDebug_liveDebug1720878949 = "testvar = testvar + 1"
platform.window:invalidate()
xpcall( OnFieldCall1600202380, OnFieldCall1600202380_Fail )
coroutine.yield()
__liveDebug_currentCodePosition_myFuncToDebug_liveDebug1720878949 = "testvar = testvar + 1"
platform.window:invalidate()
xpcall( OnFieldCall1546312225, OnFieldCall1546312225_Fail )
end)
local __errorHandleVar193951612 = ""
function OnFieldCall193951612_Fail(err)
__errorHandleVar193951612 = tostring(err)
end
function OnFieldCall193951612()
testvar = testvar + 1
platform.window:invalidate()
end
local __errorHandleVar412950835 = ""
function OnFieldCall412950835_Fail(err)
__errorHandleVar412950835 = tostring(err)
end
function OnFieldCall412950835()
testvar = testvar + 1
platform.window:invalidate()
end
function ResumeFunc_myFuncToDebug_liveDebug1720878949()
if coroutine.status(myFuncToDebug_liveDebug1720878949) == "dead" then
myFuncToDebug_liveDebug1720878949 = coroutine.create(function ()
__liveDebug_currentCodePosition_myFuncToDebug_liveDebug1720878949 = "testvar = testvar + 1"
platform.window:invalidate()
xpcall( OnFieldCall193951612, OnFieldCall193951612_Fail )
coroutine.yield()
__liveDebug_currentCodePosition_myFuncToDebug_liveDebug1720878949 = "testvar = testvar + 1"
platform.window:invalidate()
xpcall( OnFieldCall412950835, OnFieldCall412950835_Fail )
end)
end
if not coroutine.running(myFuncToDebug_liveDebug1720878949) and __liveDebug_enterPressed_myFuncToDebug_liveDebug1720878949 then
coroutine.resume(myFuncToDebug_liveDebug1720878949)
__liveDebug_enterPressed_myFuncToDebug_liveDebug1720878949 = false
end
end
function myFuncToDebug()
ResumeFunc_myFuncToDebug_liveDebug1720878949()
end
function on.paint(gc --[[Grafikgerät]])
if __errorHandleVar412950835 ~= "" then gc:drawString("[DebugError] "..__errorHandleVar412950835, 0, platform.window:height() - 20, "top") end
if __errorHandleVar193951612 ~= "" then gc:drawString("[DebugError] "..__errorHandleVar193951612, 0, platform.window:height() - 20, "top") end
if __errorHandleVar1546312225 ~= "" then gc:drawString("[DebugError] "..__errorHandleVar1546312225, 0, platform.window:height() - 20, "top") end
if __errorHandleVar1600202380 ~= "" then gc:drawString("[DebugError] "..__errorHandleVar1600202380, 0, platform.window:height() - 20, "top") end
gc:drawString("[LastCall]"..__liveDebug_currentCodePosition_myFuncToDebug_liveDebug1720878949, 0 , platform.window:height() - 20, "top")
myFuncToDebug()
gc:drawString(tostring(testvar), 50, 50, "top")
end
function on.tabKey()
__liveDebug_enterPressed_myFuncToDebug_liveDebug1720878949 = true
ResumeFunc_myFuncToDebug_liveDebug1720878949()
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
- Um das GUI in dem DevelopmentMode zu bringen, muss eine LunaGUI.debug-Datei im selben Verzeichnis existieren.
- Beim LiveDebugging müssen alle Funktionen vor der ontabKey-Funktion deklariert sein, falls eine ontabKey-Funktion vorhanden ist.
- Beim Debug-Attribut werden ScreenUpdate-Befehle (platform.window.invalidate()) hinter dem Feld mitübernommen.

##Setup
Systemanforderungen:
.Net 4.0 Framework oder neuer

Download (nur .rar):
[Windows XP/Vista/7/8/10](https://github.com/DanThePman/Luna-GUI/blob/master/Luna%20GUI/bin/x86/)