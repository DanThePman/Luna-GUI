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
- Funktions-Attribute: ScreenUpdate, Thread, Debug , LiveDebug
- Feld-Attribute: Debug


##Attribute
###ScreenUpdate
```lua
[ScreenUpdate]
function Funktionsname()
a = 6
tostring(a)
end
```
im Hintergrund generiert zu
```lua
function Funktionsname()
a = 6
tostring(a)
platform.window.invalidate()
end
```

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
###LiveDebugging - involviert StackTrace

![Image](https://raw.githubusercontent.com/DanThePman/Luna-GUI/master/liveDebugExplanation.png)
```lua
local testvar = 0

[LiveDebug]
function myFuncToDebug()
testvar = testvar + 1
platform.window:invalidate()
testvar = testvar + 2
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
local __liveDebug_currentCodePosition_myFuncToDebug_liveDebug1001730213 = "No calls of LiveDebug-Function"
local __liveDebug_currentStep_myFuncToDebug_liveDebug1001730213 = 0
local __liveDebug_enterPressed_myFuncToDebug_liveDebug1001730213 = false
local __errorHandleVar1353102104 = ""
function OnFieldCall1353102104_Fail(err)
__errorHandleVar1353102104 = tostring(err)
end
function OnFieldCall1353102104()
testvar = testvar + 1
platform.window:invalidate()
end
local __errorHandleVar1221188607 = ""
function OnFieldCall1221188607_Fail(err)
__errorHandleVar1221188607 = tostring(err)
end
function OnFieldCall1221188607()
testvar = testvar + 2
platform.window:invalidate()
end
local myFuncToDebug_liveDebug1001730213 = coroutine.create(function ()
__liveDebug_currentCodePosition_myFuncToDebug_liveDebug1001730213 = "testvar = testvar + 1"
__liveDebug_currentStep_myFuncToDebug_liveDebug1001730213 = __liveDebug_currentStep_myFuncToDebug_liveDebug1001730213 + platform.window:width()/2
platform.window:invalidate()
xpcall( OnFieldCall1353102104, OnFieldCall1353102104_Fail )
coroutine.yield()
__liveDebug_currentCodePosition_myFuncToDebug_liveDebug1001730213 = "testvar = testvar + 2"
__liveDebug_currentStep_myFuncToDebug_liveDebug1001730213 = __liveDebug_currentStep_myFuncToDebug_liveDebug1001730213 + platform.window:width()/2
platform.window:invalidate()
xpcall( OnFieldCall1221188607, OnFieldCall1221188607_Fail )
end)
local __errorHandleVar1161861833 = ""
function OnFieldCall1161861833_Fail(err)
__errorHandleVar1161861833 = tostring(err)
end
function OnFieldCall1161861833()
testvar = testvar + 1
platform.window:invalidate()
end
local __errorHandleVar1637862169 = ""
function OnFieldCall1637862169_Fail(err)
__errorHandleVar1637862169 = tostring(err)
end
function OnFieldCall1637862169()
testvar = testvar + 2
platform.window:invalidate()
end
function ResumeFunc_myFuncToDebug_liveDebug1001730213()
if coroutine.status(myFuncToDebug_liveDebug1001730213) == "dead" then
myFuncToDebug_liveDebug1001730213 = coroutine.create(function ()
__liveDebug_currentCodePosition_myFuncToDebug_liveDebug1001730213 = "testvar = testvar + 1"
__liveDebug_currentStep_myFuncToDebug_liveDebug1001730213 = __liveDebug_currentStep_myFuncToDebug_liveDebug1001730213 + platform.window:width()/2
platform.window:invalidate()
xpcall( OnFieldCall1161861833, OnFieldCall1161861833_Fail )
coroutine.yield()
__liveDebug_currentCodePosition_myFuncToDebug_liveDebug1001730213 = "testvar = testvar + 2"
__liveDebug_currentStep_myFuncToDebug_liveDebug1001730213 = __liveDebug_currentStep_myFuncToDebug_liveDebug1001730213 + platform.window:width()/2
platform.window:invalidate()
xpcall( OnFieldCall1637862169, OnFieldCall1637862169_Fail )
end)
end
if not coroutine.running(myFuncToDebug_liveDebug1001730213) and __liveDebug_enterPressed_myFuncToDebug_liveDebug1001730213 then
coroutine.resume(myFuncToDebug_liveDebug1001730213)
__liveDebug_enterPressed_myFuncToDebug_liveDebug1001730213 = false
end
end
function myFuncToDebug()
ResumeFunc_myFuncToDebug_liveDebug1001730213()
end
function onpaint(gc --[[Grafikgerät]])
if __errorHandleVar1637862169 ~= "" then gc:drawString("[DebugError] "..__errorHandleVar1637862169, 0, platform.window:height() - 40, "top") end
if __errorHandleVar1161861833 ~= "" then gc:drawString("[DebugError] "..__errorHandleVar1161861833, 0, platform.window:height() - 40, "top") end
if __errorHandleVar1221188607 ~= "" then gc:drawString("[DebugError] "..__errorHandleVar1221188607, 0, platform.window:height() - 40, "top") end
if __errorHandleVar1353102104 ~= "" then gc:drawString("[DebugError] "..__errorHandleVar1353102104, 0, platform.window:height() - 40, "top") end
__liveDebugPreviousFont_myFuncToDebug_liveDebug1001730213 = gc:setFont("serif","bi",8)
gc:drawString("[StackTrace]"..__liveDebug_currentCodePosition_myFuncToDebug_liveDebug1001730213, 0 , platform.window:height() - 10, "top")
gc:setFont(__liveDebugPreviousFont_myFuncToDebug_liveDebug1001730213,"r",10)
gc:setColorRGB(255, 0, 0)
gc:fillRect(0,0,__liveDebug_currentStep_myFuncToDebug_liveDebug1001730213,3)
gc:setColorRGB(0, 0, 0)
myFuncToDebug()
gc:drawString(tostring(testvar), 50, 50, "top")
end
function ontabKey()
__liveDebug_enterPressed_myFuncToDebug_liveDebug1001730213 = true
if __liveDebug_currentStep_myFuncToDebug_liveDebug1001730213 > platform.window:width() - 1 then __liveDebug_currentStep_myFuncToDebug_liveDebug1001730213 = 0 end
ResumeFunc_myFuncToDebug_liveDebug1001730213()
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
- Alle Attribute, die den Name "Debug" beinhalten können nur auf einzeilige Operationen angewandt werden => Schleifen als Einzeiler

##Setup
Systemanforderungen:
.Net 4.0 Framework oder neuer

Download (nur .rar):
[Windows XP/Vista/7/8/10](https://github.com/DanThePman/Luna-GUI/blob/master/Luna%20GUI/bin/x86/)