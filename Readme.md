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
local __liveDebug_enterPressed_myFuncToDebug_liveDebug684832133 = false
local __liveDebug_currentCodePosition_myFuncToDebug_liveDebug684832133 = "Not started"
local __errorHandleVar1397692820 = ""
function OnFieldCall1397692820_Fail(err)
__errorHandleVar1397692820 = tostring(err)
end
function OnFieldCall1397692820()
testvar = testvar + 1
platform.window:invalidate()
end
local __errorHandleVar683863753 = ""
function OnFieldCall683863753_Fail(err)
__errorHandleVar683863753 = tostring(err)
end
function OnFieldCall683863753()
testvar = testvar + 1
platform.window:invalidate()
end
local myFuncToDebug_liveDebug684832133 = coroutine.create(function ()
coroutine.yield()
__liveDebug_currentCodePosition_myFuncToDebug_liveDebug684832133 = "testvar = testvar + 1"
platform.window:invalidate()
xpcall( OnFieldCall1397692820, OnFieldCall1397692820_Fail )
coroutine.yield()
__liveDebug_currentCodePosition_myFuncToDebug_liveDebug684832133 = "testvar = testvar + 1"
platform.window:invalidate()
xpcall( OnFieldCall683863753, OnFieldCall683863753_Fail )
end)
local __errorHandleVar968449406 = ""
function OnFieldCall968449406_Fail(err)
__errorHandleVar968449406 = tostring(err)
end
function OnFieldCall968449406()
testvar = testvar + 1
platform.window:invalidate()
end
local __errorHandleVar662074272 = ""
function OnFieldCall662074272_Fail(err)
__errorHandleVar662074272 = tostring(err)
end
function OnFieldCall662074272()
testvar = testvar + 1
platform.window:invalidate()
end
function ResumeFunc_myFuncToDebug_liveDebug684832133()
if coroutine.status(myFuncToDebug_liveDebug684832133) == "dead" then
myFuncToDebug_liveDebug684832133 = coroutine.create(function ()
coroutine.yield()
__liveDebug_currentCodePosition_myFuncToDebug_liveDebug684832133 = "testvar = testvar + 1"
platform.window:invalidate()
xpcall( OnFieldCall968449406, OnFieldCall968449406_Fail )
coroutine.yield()
__liveDebug_currentCodePosition_myFuncToDebug_liveDebug684832133 = "testvar = testvar + 1"
platform.window:invalidate()
xpcall( OnFieldCall662074272, OnFieldCall662074272_Fail )
end)
end
if not coroutine.running(myFuncToDebug_liveDebug684832133) and __liveDebug_enterPressed_myFuncToDebug_liveDebug684832133 then
coroutine.resume(myFuncToDebug_liveDebug684832133)
__liveDebug_enterPressed_myFuncToDebug_liveDebug684832133 = false
end
end
function myFuncToDebug()
ResumeFunc_myFuncToDebug_liveDebug684832133()
end
function onpaint(gc --[[Grafikgerät]])
if __errorHandleVar662074272 ~= "" then gc:drawString("[DebugMode] "..__errorHandleVar662074272, 0, platform.window:height() - 20, "top") end
if __errorHandleVar968449406 ~= "" then gc:drawString("[DebugMode] "..__errorHandleVar968449406, 0, platform.window:height() - 20, "top") end
if __errorHandleVar683863753 ~= "" then gc:drawString("[DebugMode] "..__errorHandleVar683863753, 0, platform.window:height() - 20, "top") end
if __errorHandleVar1397692820 ~= "" then gc:drawString("[DebugMode] "..__errorHandleVar1397692820, 0, platform.window:height() - 20, "top") end
gc:drawString("[LastCall]"..__liveDebug_currentCodePosition_myFuncToDebug_liveDebug684832133, 0 , platform.window:height() - 20, "top")
myFuncToDebug()
gc:drawString(tostring(testvar), 50, 50, "top")
end
function ontabKey()
__liveDebug_enterPressed_myFuncToDebug_liveDebug684832133 = true
ResumeFunc_myFuncToDebug_liveDebug684832133()
end
```
Beinhaltet: StackTrace

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