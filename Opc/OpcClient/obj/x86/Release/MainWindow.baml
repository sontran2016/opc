   M S B A M L   `   `   `  ����   	OpcClient    OpcClient.MainWindow   \ XHardcodet.Wpf.TaskbarNotification, Version=1.0.5.0, Culture=neutral, PublicKeyToken=null)tb http://www.hardcodet.net/taskbar  R NWindowsBase, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35\ XPresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35W SPresentationCore, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35D 9http://schemas.microsoft.com/winfx/2006/xaml/presentation    R NSystem.Xaml, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     -     � Title$  
OPC Client��$	��569��$	��905�� � WindowStartupLocation$ CenterScreen=� � ShowInTaskbar .   � Icon$) #/OpcClient;component/Image/sync.ico�� � 	Resources  �� 
TargetType �� Property �� Value   HeaderGradientBrush �� EndPoint	 � MappingMode
 �� 
StartPoint � Color � Offset  ButtonGradientBrush��%  '��       &	  [     '�� �     7   &	 7    '�� �    7  �� " ��.���  � ��$ Stretch=���  � ��
 �������  � ��$ 0,0,0,1q��� $ 0.5,1&�$	 RelativeToBoundingBox=�$
 0.5,0&�.��� $ Coral��$ 1Q�� $ Bisque��$ 0.746Q��� " ��.���  � ��$ Center=���  � ��8   ��  � ��
 �������  � ��$
 Bold"��� $ 0.5,1&�$
 0.5,0&�.��� $ 	#FF1B405D��$ 1Q�� $ 	#FF2F7CA8��$ 0Q��� " ��.���  � ��
 �������  � ��8   .��]�  J� RenderTransformOrigin$ 	0.5,0.648&�$��0,0,2,30q�.+�3  -Hardcodet.Wpf.TaskbarNotification.TaskbarIcon  -   $��Notify1��   
IconSource$ Image/sync.ico��   ToolTipText$ 
Opc Client�� � ContextMenu �� .@�m� -   $��
MnuOpenApp��$��Open OPC Client�� m� Icon ��   ImgClientSource#��[ m� -   $��
MnuExitApp��$
��Exit��� $	��Top=�$	��Top=�$��38��.+��� -    ��Name$ 
btnConnect��$��Connect��$��75��$��33��$��10,0,0,0q��� -   $ btnDisConnect��$��
DisConnect��$��75��$��33��$��4,0,0,0q��� -    ��Name$ lblValue��$	��104���  � ColumnDefinitions �� $	��300 �$	��100��$	��500���� $��4 ��� $��* �.+�R� -    R�Name$ 	treeGroup��$��10,0,0,0q�
������� �� .@�m� -	   $��mnuAddGroup��$��	Add Group�� �� $��Image/add.ico��m� -
   $��mnuRemoveGroup��$��Remove Group�� �� $��Image/delete.ico��m� -   $��mnuAddServer��$��
Add Server��m� -   $��mnuRemoveServer��$��Remove Server���� $��1��$��5��$��Stretch=��� -    ��Name$ lvTag��$��2��$��Right=�$��0,0,10,0q�����  �� .@�m� -   $��mnuSetValue��$��	Set Value�� �� $��Image/edit.ico��m� -   $��	mnuAddTag��$��Add Tag�� �� $��Image/add.ico��m� -   $��mnuRemoveTag��$��
Remove Tag�� �� $��Image/delete.ico�� �� View �� .P���  �� CellTemplate �� .R�� .+��� $��16������ *	IconRow+��  �� Width$ 0��$��Id�� �� DisplayMemberBinding �� *Id+�� $��Tag Name��$	 200�� �� *Name+�� $	 100��.���� $��Value�� �� .R���  �� TextAlignment$ Center=����� *Value+�� $��Quality��$	 120�� �� *	Quality+