﻿using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using WinApi;

namespace GUITest
{
    public static class UI
    {
        static Form main;

        public static void Initialize(Type mainFormType)
        {
            main = WaitForForm(mainFormType, 60);
        }

        public static void Initialize(string mainFormNameOrType)
        {
            main = WaitForForm(mainFormNameOrType);
        }

        public static Form WaitForForm(string nameOrType, double timeout = 3.0, double interval = 0.1)
        {
            var due = DateTime.Now.AddSeconds(timeout);
            while (DateTime.Now.CompareTo(due) < 0)
            {
                var form = FindForm(nameOrType);
                if (form != null)
                    return form;
                Thread.Sleep((int)(1000 * interval));
            }
            return null;
        }

        internal static void Type(string text, double delay = 0.1)
        {
            if (text.StartsWith("{"))
            {
                SendKeys.SendWait(text);
            }
            else
            {
                foreach(var c in text)
                {
                    SendKeys.SendWait(c.ToString());
                    Thread.Sleep((int)(delay * 1000));
                }
            }
        }

        public static Form WaitForForm(Type type, double timeout = 3.0, double interval = 0.1)
        {
            var due = DateTime.Now.AddSeconds(timeout);
            while (DateTime.Now.CompareTo(due) < 0)
            {
                var form = FindForm(type);
                if (form != null)
                    return form;
                Thread.Sleep((int)(1000 * interval));
            }
            return null;
        }

        public static Control GetControl(string path, int maxLevel = int.MaxValue)
        {
            var parts = path.Split('.');

            if (parts.Length == 0)
                return null;

            var form = FindForm(parts[0]);
            ThrowIfNull(form, "Form not found: " + parts[0]);

            if (parts.Length == 1)
                return form;

            Control parent = form;
            Control member = null;
            for (int i = 1; i < parts.Length; ++i)
            {
                member = FindControl(parent, parts[i]);
                if (member == null)
                    return null;

                parent = member;
            }

            ThrowIfNull(member, String.Format("Member {0} not found", path));
            return member;
        }

        public static T GetMember<T>(string path, Type type, int maxLevel = int.MaxValue) where T : class
        {
            return GetMember(path, type, maxLevel = int.MaxValue) as T;
        }

        public static object GetMember(string path, Type type, int maxLevel = int.MaxValue)
        {
            var parts = path.Split('.');

            if (parts.Length == 0)
                return null;

            var form = FindForm(parts[0]);
            ThrowIfNull(form, "Form not found: " + parts[0]);

            if (parts.Length == 1)
                return form;

            object parent = form;
            object member = null;
            for (int i = 1; i < parts.Length; ++i)
            {
                member = FindMember(parent, parts[i]);
                if (member == null)
                    return null;

                parent = member;
            }

            ThrowIfNull(member, String.Format("Member {0} not found", path));
            return member;
        }

        internal static void Close()
        {
            if (main != null)
                Perform(() => { main.Close(); });
        }

        public static Form FindForm(Type type)
        {
            foreach (Form form in Application.OpenForms)
                if (type == null || form.GetType().Equals(type))
                    return form;
            return null;
        }

        public static Form FindForm(string nameOrType)
        {
            foreach (Form form in Application.OpenForms)
                if (nameOrType == null || nameOrType.Equals(form.Name) || nameOrType.Equals(form.Text))
                    return form;

            foreach (Form form in Application.OpenForms)
            {
                var typeName = form.GetType().Name;
                if (typeName.Equals(nameOrType) || typeName.EndsWith("." + nameOrType))
                    return form;
            }

            return null;
        }

        public static Control FindControl(Control instance, string name, int maxLevel = 1)
        {
            if (instance is ContainerControl)
                foreach (Control control in ((ContainerControl)instance).Controls)
                    if (name.Equals(control.Name))
                        return control;

            return null;
        }

        public static object FindMember(object instance, string name, Type type = null, int maxLevel = 1)
        {
            var field = FindField(instance, name, maxLevel);
            if (field != null)
            {
                var value = field.GetValue(instance);
                if (value != null || value.GetType().Equals(type))
                    return value;
            }
            return null;
        }

        public static FieldInfo FindField(object instance, string name, int maxLevel = 1)
        {
            if (instance == null)
                return null;

            var type = instance.GetType();
            var fields = type.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.FlattenHierarchy);

            foreach (var field in fields)
                if (field.Name.Equals(name))
                    return field;

            return null;
        }

        public static void Delay(double interval, Action action)
        {
            System.Threading.Timer timer = null;
            timer = new System.Threading.Timer((e) => { action(); });
            timer.Change((int)(1000 * interval), int.MaxValue);
        }

        // Use this method to execute your action in the UI thread.
        public static void Perform(Action action)
        {
            ThrowIfNull(main, "UI not initialized - root control is null.");
            main.UIThread(action);
        }

        public static void ThrowIfNull(object instance, string message = "Instance not found")
        {
            if (instance == null)
                throw new ApplicationException(message);
        }

        public static void Type(char c)
        {
            var inputs = CreateSingleInput(c);
            Win32.SendInput((uint)inputs.Count, inputs.ToArray(), INPUT.Size);
        }

        private static List<INPUT> CreateSingleInput(char character)
        {
            var inputs = new List<INPUT>();
            inputs.Add(new INPUT()
            {
                type = InputType.KEYBOARD,
                U = new InputUnion()
                {
                    ki = new KEYBDINPUT()
                    {
                        wVk = 0,
                        wScan = (ScanCodeShort)character,
                        dwFlags = KEYEVENTF.UNICODE
                    }
                }
            });
            inputs.Add(new INPUT()
            {
                type = InputType.KEYBOARD,
                U = new InputUnion()
                {
                    ki = new KEYBDINPUT()
                    {
                        wVk = 0,
                        wScan = (ScanCodeShort)character,
                        dwFlags = KEYEVENTF.UNICODE | KEYEVENTF.KEYUP
                    }
                }
            });
            return inputs;
        }
    }
}
