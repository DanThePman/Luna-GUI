<img src="icon.png" align="right" />
# Luna GUI
Das Luna GUI sorgt f�r die Aktualisierung von Code-Snippets innerhalb der Sublime-Text-Entwicklungsumgebung, um effizienter Code 
fuer den Taschenrechner zu erstellen und besitzt einen integrierten Compiler, der das Erstellen und �bertragen von tns-Dateien 
erm�glicht.
Da das Drag&Drop-Verfahren simuliert wird m�ssen Positionen f�r Buttons und Felder festgelegt werden.
Ein OffsetFinder-Wizard im Tab "Testing" hilft beim Ermitteln der relativen Fenster- und Buttonpositionen, die gesetzt werden m�ssen,
um Dateien automatisch auf den Emulator �bertragen zu k�nnen.

Hinweise:
- Reload-Button => Gelber Button, der im Dateiuebertragungsfenster steht
- Zielordner-Textfeld => unter Einstellungen->Datei�bertragung das Textfeld neben "Ziel-Ordner"
- Taskleistensymbol => von Firebird


## CodeAnalyse
Aktuelle Funktionen:
- MoonSharp Lua-Compiler
- Eventkonvertierung
- Deklarations�berpr�fung
- Funktionsattribute: (Ursprung: Microsoft.Reflection) ScreenUpdate, Debug
- Feldattribute: (Ursprung: Microsoft.Reflection) Debug

Zuf�nftige:
- Multithreading-Attribut
- Korrekte Verwendung des Grafikkontextes "gc"


##Attribute
Attribut-Beispiel:
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
=======> wird im Hintergrund generiert zu =====>
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

## Setup
Systemanforderungen:
.Net 4.0 Framework oder neuer

Download (nur .rar):
[Windows XP/Vista/7/8/10](https://github.com/DanThePman/Luna-GUI/blob/master/Luna%20GUI/bin/x86/)



##Fehler bei Windows XP
Unable to cast COM object of type System.__ComObject to interface type X 
- => L�sung:
- regsvr32 "C:\Program Files\Internet Explorer\ieproxy.dll"

HttpWebrequests k�nnen nicht zu Github aufgebaut werden.
- => Keine Codesnippet-Updates

##Hinweise
Um das GUI in dem DevelopmentMode zu bringen, muss eine LunaGUI.debug-Datei im selben Verzeichnis existieren.