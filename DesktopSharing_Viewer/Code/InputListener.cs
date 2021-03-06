﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;

namespace DesktopSharing_Viewer.Code
{
    public class InputListener : System.Windows.Forms.IMessageFilter
    {
        public event Desktop_Sharing_Shared.Mouse.PInvoke.MouseEventHandler InputMouseEvent;
        public event Desktop_Sharing_Shared.Keyboard.PInvoke.KeyEventHandler InputKeyEvent;
        private DateTime KeyboardSecondCounter = DateTime.Now;
        private DateTime MouseSecondCounter = DateTime.Now;

        private const int InputPerSec = 30;
        private List<int> Keys_Down;
        private int _LastMsg, _LastX, _LastY, _Lastwheel;

        private IntPtr Handle;
        public InputListener(IntPtr handle)
        {
            Handle = handle;
            Keys_Down = new List<int>();
            _LastMsg = _LastX = _LastY = _Lastwheel = 0;
        }

        public bool PreFilterMessage(ref System.Windows.Forms.Message m)
        {
            if(Handle == m.HWnd)
            {
                if(m.Msg == Desktop_Sharing_Shared.Keyboard.PInvoke.WM_KEYDOWN || m.Msg == Desktop_Sharing_Shared.Keyboard.PInvoke.WM_KEYUP)
                {
                    if(InputKeyEvent != null)
                    {

                        var temp = unchecked(IntPtr.Size == 8 ? (int)m.WParam.ToInt64() : (int)m.WParam.ToInt32());

                        if(m.Msg == Desktop_Sharing_Shared.Keyboard.PInvoke.WM_KEYDOWN)
                        {
                            //Debug.WriteLine("KeyDown");
                            if(Keys_Down.Contains(temp))
                            {
                                if((DateTime.Now - KeyboardSecondCounter).TotalMilliseconds < InputPerSec)
                                    return false;
                                else
                                    KeyboardSecondCounter = DateTime.Now;
                            } else
                            {
                                Keys_Down.Add(temp);
                            }
                        } else
                        {
                            //Debug.WriteLine("KeyUP");
                            Keys_Down.Remove(temp);//else its an up, so remove it
                        }
                        InputKeyEvent(new Desktop_Sharing_Shared.Keyboard.KeyboardEventStruct
                        {
                            bVk = temp,
                            s = m.Msg == Desktop_Sharing_Shared.Keyboard.PInvoke.WM_KEYDOWN ? Desktop_Sharing_Shared.Keyboard.PInvoke.PInvoke_KeyState.DOWN : Desktop_Sharing_Shared.Keyboard.PInvoke.PInvoke_KeyState.UP
                        });

                        return true;
                    }

                }
                if(InputMouseEvent != null && ((int[])Enum.GetValues(typeof(Desktop_Sharing_Shared.Mouse.PInvoke.WinFormMouseEventFlags))).Contains(m.Msg))
                {
                    if(m.Msg == Desktop_Sharing_Shared.Mouse.PInvoke.WM_MOUSEMOVE)
                    {
                        if((DateTime.Now - MouseSecondCounter).TotalMilliseconds < InputPerSec)
                            return false;
                        else
                            MouseSecondCounter = DateTime.Now;
                    }
                    var p = GetPoint(m.LParam);
                    var wheel = 0;
                    if(m.Msg == Desktop_Sharing_Shared.Mouse.PInvoke.WM_MOUSEWHEEL)
                    {
                        uint xy = unchecked(IntPtr.Size == 8 ? (uint)m.WParam.ToInt64() : (uint)m.WParam.ToInt32());
                        wheel = unchecked((short)(xy >> 16));
                    }
                    if(_LastMsg != m.Msg || p.X != _LastX || p.Y != _LastY || _Lastwheel != wheel)
                    {
                        InputMouseEvent(new Desktop_Sharing_Shared.Mouse.MouseEventStruct
                        {
                            msg = (Desktop_Sharing_Shared.Mouse.PInvoke.WinFormMouseEventFlags)m.Msg,
                            x = p.X,
                            y = p.Y,
                            wheel_delta = wheel

                        });

                        _LastMsg = m.Msg;
                        p.X = _LastX;
                        p.Y = _LastY;
                        _Lastwheel = wheel;
                    }

                }
                // Debug.WriteLine("Mouse Event");
            }
            return false;
        }
        private int GetInt(IntPtr ptr)
        {
            return IntPtr.Size == 8 ? unchecked((int)ptr.ToInt64()) : ptr.ToInt32();
        }
        private Point GetPoint(IntPtr _xy)
        {
            uint xy = unchecked(IntPtr.Size == 8 ? (uint)_xy.ToInt64() : (uint)_xy.ToInt32());
            int x = unchecked((short)xy);
            int y = unchecked((short)(xy >> 16));
            return new Point(x, y);
        }
    }
}
