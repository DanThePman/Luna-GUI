   M S B A M L   `   `   `  ����T  PMahApps.Metro, Version=1.2.4.0, Culture=neutral, PublicKeyToken=f4fb5a3c4d1e5b4fV;clr-namespace:MahApps.Metro.Controls;assembly=MahApps.MetroMahApps.Metro.Controls  5       Luna GUI   Luna_GUI.MainWindow   5      W SPresentationCore, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35R NWindowsBase, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35\ XPresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35D 9http://schemas.microsoft.com/winfx/2006/xaml/presentation    5   	   R NSystem.Xaml, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     5   	   @d2http://schemas.microsoft.com/expression/blend/2008     5   	   Jmc;http://schemas.openxmlformats.org/markup-compatibility/2006     5   	   PControls;clr-namespace:MahApps.Metro.Controls;assembly=MahApps.Metro     5      -   5        (   "MahApps.Metro.Controls.MetroWindow   Name$  
mainWindow��5       � Title$ Luna GUI��5   	   $	��350��6   $	��525��6'    � 
ResizeMode$ CanMinimize=�6N   .��5	      � .+�5
   
   4   .MahApps.Metro.Controls.MetroAnimatedTabControl  -   5          Name$ 
tabControl��5
   +   $
��Left=�6?   $	��321��6Z   $	��Top=�6g   $	��519��6    �� SelectedIndex$ 0��6�   .@�5      �� $
��Menu��6   $��10,10,-10,-10q�6$   $
��Left=�6;   $��80��6V   .��5      � .+�5      )   #MahApps.Metro.Controls.ProgressRing  -   5        $��	progress1��5   T      IsActive .  $y�	Collapsed=�6=   $��10,215,453,0q�6e   $��61��6{   $��50��6�   &   System.Windows.Controls.DataGrid  5      -   5          Name$ localSnippetGrid��5      $
��Left=�69   $��	10,20,0,0q�6T   $	��Top=�6g   $	��196��6   $	��493��6�      AutoGenerateColumns .    
IsReadOnly . ��5      �� 	 �� Path$	 LocalSnippetCollection�5   .   �� 5      -   5        
 ��Name$
 checkButton_Copy��5      $��Aktualisiere��67   $
��Left=�6N   $��374,241,0,0q�6i   $	��Top=�6~   $	��129��6�   6�   5      5      �� 5      -   5         ��Name$ codeChangeTab��5      $��Code Änderungen��6-   $��23,10,-24,-10q�6F   .��5      � $��0,1,0,-1q�6   .+�5      �� $
��Left=�6    $	��100��6;   $��
127,91,0,0q�6H   $	��Top=�6\   $	��266��6t    J� RenderTransformOrigin$ 0.5,0.5&�6�   z�5      V� 5      .~�5   "   �� �� 5   "   �� 5   "    �� Angle$ 90Q�62   T� 5   "   5      5      �� 5       -   5         ��Name$ label��5       $
��Left=�6+   $��
131,71,0,0q�6F   $	��Top=�6Z   �� 5!      -   5         ��Name$ richTextBox1��5!   "   $
��Left=�68   $	��262��6S   $��	10,10,0,0q�6`   $	��Top=�6s   $	��244��6�    �� 
IsReadOnly . . �5"      /�  � 	Resources 5#       �� 
TargetType �� Property �� Value��%X   'J�       �� 5$      " J�6$   .�5%   "   ��  �@��Margin$ 0q�6;   5&      5'      5(      �� 5)      -	   5        $ richTextBox2��5)   "   $
��Left=�68   $	��262��6S   $��
265,10,0,0q�6`   $	��Top=�6t   $	��244��6�    . . �5*      /�  5+      ��%X   'J�       �� 5,      " J�6$   .�5-   "   ��  �@��Margin$ 0q�6;   5.      5/      50      51      52      �� 53      -
   5        $ compilingTab��53      $��Testing��6,   $��25,10,-22,-10q�6=   .��54      � $��	0,15,0,-1q�6   .+�55      �� -   5         ��Name$ checkBox��55      $
��Left=�61   $	��Top=�6L   $��431,210,0,0q�6d   $��78��6y   $.��(Overclock                               ��6�   {�.  56   !   �� 57      -   5        $
 oncompileDragDropButton��57      $��Lade Datei auf den Cx Cas��6>   $
��Left=�6b   $��357,233,0,0q�6}   $	��Top=�6�   $	��152��6�   6�   �� 58      -   5        $ luapathLabel��58      $�� ��62   $��154,10,10,233q�6=   �� 59      -   5        $
 luaFileButton��59      $��Lua-Datei auswählen...��64   $
��Left=�6U   $��7,10,0,0q�6p   $	��Top=�6�   $	��142��6�   $��12��6�   6�   �� 5:      -   5        $
 StartOffsetAssistent��5:      $"��Starte Positions-Assistenten��6;   $
��Left=�6b   $��
10,106,0,0q�6}   $	��Top=�6�   $	��173��6�   $��12��6�   6�   �� 5;      -   5        $ positionInfoLabel��5;      $��Positionen: ��67   $
��Left=�6N   $��	10,75,0,0q�6i   $	��Top=�6|   $	��173��6�   �� 5<      -   5        $
 onCompileButton��5<      $��Nur kompilieren��66   $
��Left=�6P   $��	7,233,0,0q�6k   $	��Top=�6~   $	��142��6�   6�   5=      5>      5@      5A      5B      