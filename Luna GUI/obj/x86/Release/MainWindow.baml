   M S B A M L   `   `   `  ���� T  PMahApps.Metro, Version=1.2.4.0, Culture=neutral, PublicKeyToken=f4fb5a3c4d1e5b4fV;clr-namespace:MahApps.Metro.Controls;assembly=MahApps.MetroMahApps.Metro.Controls   Luna GUI   Luna_GUI.MainWindow   W SPresentationCore, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35R NWindowsBase, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35\ XPresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35D 9http://schemas.microsoft.com/winfx/2006/xaml/presentation    R NSystem.Xaml, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     @d2http://schemas.microsoft.com/expression/blend/2008     Jmc;http://schemas.openxmlformats.org/markup-compatibility/2006     PControls;clr-namespace:MahApps.Metro.Controls;assembly=MahApps.Metro     -   (   "MahApps.Metro.Controls.MetroWindow   Name$  
mainWindow�� � Title$ Luna GUI��$	��350��$	��525�� � 
ResizeMode$ CanMinimize=�.��� .+�4   .MahApps.Metro.Controls.MetroAnimatedTabControl  -     Name$ 
tabControl��$
��Left=�$	��321��$	��Top=�$	��519�� �� SelectedIndex$ 0��.@��� $
��Menu��$��10,10,-10,-10q�$
��Left=�$��80��.��� .+�)   #MahApps.Metro.Controls.ProgressRing  -   $��	progress1��   IsActive .  $y�	Collapsed=�$��10,215,453,0q�$��61��$��50��&   System.Windows.Controls.DataGrid  -     Name$ localSnippetGrid��$
��Left=�$��	10,20,0,0q�$	��Top=�$	��196��$	��493��   AutoGenerateColumns .    
IsReadOnly . ���� 	 �� Path$	 LocalSnippetCollection��� -   
 ��Name$
 checkButton_Copy��$��Aktualisiere��$
��Left=�$��374,241,0,0q�$	��Top=�$	��129���� -    ��Name$ codeChangeTab��$��Code Änderungen��$��23,10,-24,-10q�.��� $��0,1,0,-1q�.+��� $
��Left=�$	��100��$��
127,91,0,0q�$	��Top=�$	��266�� J� RenderTransformOrigin$ 0.5,0.5&�z�V� .~��� �� ��  �� Angle$ 90Q�T� �� -    ��Name$ label��$
��Left=�$��
131,71,0,0q�$	��Top=��� -    ��Name$ richTextBox1��$
��Left=�$	��262��$��	10,10,0,0q�$	��Top=�$	��244�� �� 
IsReadOnly . . �/�  � 	Resources  �� 
TargetType �� Property �� Value��%3   'J�       �� " J�.���  �@��Margin$ 0q��� -	   $ richTextBox2��$
��Left=�$	��262��$��
265,10,0,0q�$	��Top=�$	��244�� . . �/�  ��%3   'J�       �� " J�.���  �@��Margin$ 0q��� -
   $ compilingTab��$��Testing��$��25,10,-22,-10q�.��� $��	0,15,0,-1q�.+��� -    ��Name$ checkBox��$
��Left=�$	��Top=�$��431,210,0,0q�$��78��$.��(Overclock                               ��{�.  �� -   $
 oncompileDragDropButton��$��Lade Datei auf den Cx Cas��$
��Left=�$��357,233,0,0q�$	��Top=�$	��152���� -   $ luapathLabel��$�� ��$��154,10,10,233q��� -   $
 luaFileButton��$��Lua-Datei auswählen...��$
��Left=�$��7,10,0,0q�$	��Top=�$	��142��$��12���� -   $
 StartOffsetAssistent��$"��Starte Positions-Assistenten��$
��Left=�$��
10,106,0,0q�$	��Top=�$	��173��$��12���� -   $ positionInfoLabel��$��Positionen: ��$
��Left=�$��	10,75,0,0q�$	��Top=�$	��173���� -   $
 onCompileButton��$��Nur kompilieren��$
��Left=�$��	7,233,0,0q�$	��Top=�$	��142��