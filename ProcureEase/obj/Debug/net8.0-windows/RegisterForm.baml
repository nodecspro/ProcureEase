   M S B A M L   `   `   `  ����T  PMahApps.Metro, Version=2.0.0.0, Culture=neutral, PublicKeyToken=51482d6f650b2b3fV;clr-namespace:MahApps.Metro.Controls;assembly=MahApps.MetroMahApps.Metro.Controls  5       ProcureEase   ProcureEase.RegisterForm   5      W SPresentationCore, Version=8.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35\ XPresentationFramework, Version=8.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35e aSystem.Windows.Controls.Ribbon, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089R NWindowsBase, Version=8.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35^ ZWindowsFormsIntegration, Version=8.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35H 9http://schemas.microsoft.com/winfx/2006/xaml/presentation      5      R NSystem.Xaml, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     5      Kmah;clr-namespace:MahApps.Metro.Controls;assembly=MahApps.Metro     5      @d2http://schemas.microsoft.com/expression/blend/2008     5      Jmc;http://schemas.openxmlformats.org/markup-compatibility/2006     5      ] YXceed.Wpf.Toolkit.NET5, Version=4.6.0.0, Culture=neutral, PublicKeyToken=3e4669d2f30244f44xctk)http://schemas.xceed.com/wpf/xaml/toolkit  5      -   5          � Title$  Регистрация��5	      $	��400��5
      $	��640��5       � Icon$/ )pack://application:,,,/Source/AppIcon.png��5       � 
ResizeMode$ NoResize=�5       � WindowStartupLocation$ CenterScreen=�5      (   "MahApps.Metro.Controls.MetroWindow   WindowTransitionsEnabled$ False��5       � 	Resources 5      %  Xceed.Wpf.Toolkit.MaskedTextBox �� 
TargetType �� BasedOn��%0   '        7���� 5   
   "  6   8   60   5      .��5      � $��15q�6    � RowDefinitions 5   
   �� 5      $
��Auto �6   �� 5      $��* �6   �� 5      $
��Auto �6   .+�5   
   �� $	��0,8q�5      $��Center=�5      $��Center=�5      $��18(�5      $
��Bold"�5       Регистрация5"      �� 5#   
   $��1��6   $��8q�6"   .+�5$      �� $	��0,2q�6   $&�� Имя пользователя:��6%   �� 5%      -   5        	 ��Name$	 TxtUsername��5&      $	��0,4q�5'      $��4q�5(      $��0,0,0,1q�5)      *   $MahApps.Metro.Controls.TextBoxHelper
   	Watermark$
 
Логин��5*         ClearTextButton$
 True��5+      5,      �� 5-      $��0,6,0,2q�6   $��Пароль:��6)   G� 5.      -   5         G�Name$ TxtPassword��5/      $	��0,4q�50      $��4q�51      $��0,0,0,1q�52      $
 Пароль��53      $
 True��54      55      �� 56      $��0,6,0,2q�6   $��Имя:��6)   �� 57      -   5        $	 TxtFirstName��58      $	��0,4q�59      $��4q�5:      $��0,0,0,1q�5;      $
 Имя��5<      $
 True��5=      5>      �� 5?      $��0,6,0,2q�6   $��Фамилия:��6)   �� 5@      -   5        $	 TxtLastName��5A      $	��0,4q�5B      $��4q�5C      $��0,0,0,1q�5D      $
 Фамилия��5E      $
 True��5F      5G      �� 5H      $��0,6,0,2q�6   $+��%Отчество (если есть):��6)   �� 5I      -   5        $	 TxtPatronymic��5J      $	��0,4q�5K      $��4q�5L      $��0,0,0,1q�5M      $
 Отчество��5N      $
 True��5O      5P      �� 5Q      $��0,6,0,2q�6   $"��Номер телефона:��6)     5R      -   5          Name$ TxtPhoneNumber��5S      $	��0,4q�5T      $��4q�5U      $��0,0,0,1q�5V         Mask$ +7 (000) 000-00-00��5W      $
 True��5X      5Y      �� 5Z      $��0,6,0,2q�6   $��Почта:��6)   �� 5[      -   5        $	 TxtEmail��5\      $	��0,4q�5]      $��4q�5^      $��0,0,0,1q�5_      $
 
Почта��5`      $
 True��5a      5b      �� 5c      $��0,6,0,2q�6   $$��Код приглашения:��6)   �� 5d      -	   5        $	 TxtInviteCode��5e      $	��0,4q�5f      $��4q�5g      $��0,0,0,1q�5h      $#
 Код приглашения��5i      $
 True��5j      5k      5l      �� 5m   
   -
   5        $��1��5n      $��2��5o      $��	0,476,0,0q�5p      $��4q�5q      $��Center=�5r      $	��Top=�5s      $*��$Зарегистрироваться��5u       �� 	IsDefault$
 True��5v      �� 5w   
   -   5        $��2��5x      $��0,24,0,8q�5y      $��Center=�5z       � Cursor$
 Hand��5{      $
��Bold"�5|      
����d�$��
Назад��5      $��	Underline{�5�      5�      5�      