using Microsoft.DirectX;
using Microsoft.DirectX.DirectInput;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace TGC.Group.Libs
{
    public class MyTgcInput
    {
        /// <summary>
        ///     Botones del mouse para DirectInput
        /// </summary>
        public enum MouseButtons
        {
            BUTTON_LEFT = 0,
            BUTTON_RIGHT = 1,
            BUTTON_MIDDLE = 2
        }

        public enum XB360Digital
        {
            BUTTON_A = 0,
            BUTTON_B = 1,
            BUTTON_X = 2,
            BUTTON_Y = 3,
            SHOULDER_L = 4,
            SHOULDER_R = 5,
            BUTTON_BACK = 6,
            BUTTON_START = 7,
            THUMB_LEFT = 8, //boton del stick izquierdo
            THUMB_RIGHT = 9 //boton del stick derecho
        }

        public enum XB360Analog
        {
            STICK_LX, //eje X de stick izquierdo
            STICK_LY, //eje Y de stick izquierdo
            STICK_RX, //eje X de stick derecho
            STICK_RY, //eje Y de stick derecho
            TRIGGER_L, //gatillo izquierdo
            TRIGGER_R //gatillo derecho
        }

        private const int HISTORY_BUFFER_SIZE = 10;
        private const float WEIGHT_MODIFIER = 0.2f;
        private readonly Point ceroPoint = new Point(0, 0);
        private bool[] currentkeyboardState;
        private bool[] currentMouseButtonsState;
        private bool[] currentJoystickState;
        private Vector2[] historyBuffer;

        //Keyboard
        private Device keyboardDevice;

        //Mouse
        private Device mouseDevice;

        //Joystick
        private Device joystickDevice;
        private bool joystickConected { get; set; }
        private int joystick_L_deadzone { get; set; } //deadzone para stick izquierdo
        private int joystick_R_deadzone { get; set; } //deadzone para stick derecho
        private int joystick_TRIGGER_deadzone { get; set; } //deadzone para gatillos

        private int mouseIndex;
        private bool mouseInside;

        private Vector2[] mouseMovement;
        private int mouseX;
        private int mouseY;
        private Control panel3d;

        private bool[] previouskeyboardState;

        private bool[] previousJoystickState;

        private bool[] previousMouseButtonsState;

        public void Initialize(Control guiControl, Control panel3d)
        {
            this.panel3d = panel3d;

            //keyboard
            keyboardDevice = new Device(SystemGuid.Keyboard);
            keyboardDevice.SetCooperativeLevel(guiControl,
                CooperativeLevelFlags.Background | CooperativeLevelFlags.NonExclusive);
            keyboardDevice.Acquire();

            //mouse
            mouseDevice = new Device(SystemGuid.Mouse);
            mouseDevice.SetCooperativeLevel(guiControl,
                CooperativeLevelFlags.Background | CooperativeLevelFlags.NonExclusive);
            mouseDevice.Acquire();
            mouseIndex = 0;
            EnableMouseSmooth = true;
            WeightModifier = WEIGHT_MODIFIER;
            mouseX = 0;
            mouseY = 0;

            init_joystick(guiControl);

            //Inicializar mouseMovement
            mouseMovement = new Vector2[2];
            for (var i = 0; i < mouseMovement.Length; i++)
            {
                mouseMovement[i] = new Vector2(0.0f, 0.0f);
            }

            //Inicializar historyBuffer
            historyBuffer = new Vector2[HISTORY_BUFFER_SIZE];
            for (var i = 0; i < historyBuffer.Length; i++)
            {
                historyBuffer[i] = new Vector2(0.0f, 0.0f);
            }

            //Inicializar ubicacion del cursor
            var ceroToScreen = this.panel3d.PointToScreen(ceroPoint);
            Cursor.Position = new Point(ceroToScreen.X + panel3d.Width / 2, ceroToScreen.Y + panel3d.Height / 2);
            mouseInside = checkMouseInsidePanel3d();

            //Inicializar estados de teclas
            var keysArray = (int[])Enum.GetValues(typeof(Key));
            var maxKeyValue = keysArray[keysArray.Length - 1];
            previouskeyboardState = new bool[maxKeyValue];
            currentkeyboardState = new bool[maxKeyValue];
            for (var i = 0; i < maxKeyValue; i++)
            {
                previouskeyboardState[i] = false;
                currentkeyboardState[i] = false;
            }

            //Inicializar estados de botones del mouse
            previousMouseButtonsState = new bool[3];
            currentMouseButtonsState = new bool[previousMouseButtonsState.Length];
            for (var i = 0; i < previousMouseButtonsState.Length; i++)
            {
                previousMouseButtonsState[i] = false;
                currentMouseButtonsState[i] = false;
            }
            init_joystick(guiControl);
        }

        internal void destroy()
        {
            keyboardDevice.Unacquire();
            keyboardDevice.Dispose();

            mouseDevice.Unacquire();
            mouseDevice.Dispose();

            joystickDevice.Unacquire();
            joystickDevice.Dispose();
        }

        public void update()
        {
            //Ver si el cursor esta dentro del panel3d
            var currentInside = checkMouseInsidePanel3d();

            //Si esta afuera y antes estaba adentro significa que salio. No capturar ningun evento, fuera de jurisdiccion
            if (mouseInside && !currentInside)
            {
                mouseInside = false;
            }

            //Ahora esta adentro, capturar eventos
            else if (currentInside)
            {
                //Estaba afuera y ahora esta adentro, hacer foco en panel3d para perder foco de algun control exterior
                if (!mouseInside)
                {
                    panel3d.Focus();
                }

                mouseInside = true;

                updateKeyboard();
                updateMouse();
                if (joystickConected) { updateJoystick(); }

                //Terminar ejemplo
                if (keyPressed(Key.Escape))
                {
                    //GuiController.Instance.stopCurrentExample();
                }
            }
        }

        private bool checkMouseInsidePanel3d()
        {
            //Obtener mouse X, Y absolute
            var ceroToScreen = panel3d.PointToScreen(ceroPoint);
            mouseX = Cursor.Position.X - ceroToScreen.X;
            mouseY = Cursor.Position.Y - ceroToScreen.Y;

            //Ver si el cursor esta dentro del panel3d
            return panel3d.ClientRectangle.Contains(mouseX, mouseY);
        }

        internal void updateKeyboard()
        {
            var state = keyboardDevice.GetCurrentKeyboardState();

            //Hacer copia del estado actual
            Array.Copy(currentkeyboardState, previouskeyboardState, currentkeyboardState.Length);

            //Actualizar cada tecla del estado actual
            for (var i = 0; i < currentkeyboardState.Length; i++)
            {
                var k = (Key)(i + 1);
                currentkeyboardState[i] = state[k];
            }
        }

        internal void updateMouse()
        {
            var mouseState = mouseDevice.CurrentMouseState;

            //Hacer copia del estado actual
            Array.Copy(currentMouseButtonsState, previousMouseButtonsState, currentMouseButtonsState.Length);

            //Actualizar estado de cada boton
            var mouseStateButtons = mouseState.GetMouseButtons();
            currentMouseButtonsState[(int)MouseButtons.BUTTON_LEFT] =
                mouseStateButtons[(int)MouseButtons.BUTTON_LEFT] != 0;
            currentMouseButtonsState[(int)MouseButtons.BUTTON_MIDDLE] =
                mouseStateButtons[(int)MouseButtons.BUTTON_MIDDLE] != 0;
            currentMouseButtonsState[(int)MouseButtons.BUTTON_RIGHT] =
                mouseStateButtons[(int)MouseButtons.BUTTON_RIGHT] != 0;

            //Mouse X, Y relative
            if (EnableMouseSmooth)
            {
                performMouseFiltering(mouseState.X, mouseState.Y);
                performMouseSmoothing(XposRelative, YposRelative);
            }
            else
            {
                XposRelative = mouseState.X;
                YposRelative = mouseState.Y;
            }

            //Mouse Wheel
            if (mouseState.Z > 0)
            {
                WheelPos = 1.0f;
            }
            else if (mouseState.Z < 0)
            {
                WheelPos = -1.0f;
            }
            else
            {
                WheelPos = 0.0f;
            }
        }

        internal void updateJoystick()
        {
            //Get Joystick State.
            JoystickState state = joystickDevice.CurrentJoystickState;
            previousJoystickState = currentJoystickState;
            //Capture Buttons.
            byte[] buttons = state.GetButtons();
            for (int i = 0; i < buttons.Length; i++)
            {
                //Console.Out.Write(buttons[i]);
                if (buttons[i] > 0)
                {
                    currentJoystickState[i] = true;
                } else
                {
                    currentJoystickState[i] = false;
                }
            }
            
        }

        public void init_joystick(Control guiControl)
        {
            //create joystick device.
            foreach (
                DeviceInstance di in
                Manager.GetDevices(
                    DeviceClass.GameControl,
                    EnumDevicesFlags.AttachedOnly))
            {
                joystickDevice = new Device(di.InstanceGuid);
                Console.Out.Write("Detectado\n");
                Console.Out.Write("En GUID:");
                Console.Out.Write(di.InstanceGuid);
                Console.Out.Write("\n");
                break;
            }

            if (joystickDevice == null)
            {
                //Throw exception if joystick not found.
                //throw new Exception("No joystick found.");
                Console.Out.Write("No joystick found");
                joystickConected = false;
            }
            else
            {

                foreach (DeviceObjectInstance doi in joystickDevice.Objects)
                {
                    if ((doi.ObjectId & (int)DeviceObjectTypeFlags.Axis) != 0)
                    {
                        joystickDevice.Properties.SetRange(
                            ParameterHow.ById,
                            doi.ObjectId,
                            new InputRange(-5000, 5000));
                    }
                }

                //Set joystick axis mode absolute.
                joystickDevice.Properties.AxisModeAbsolute = true;

                joystickDevice.SetCooperativeLevel(guiControl,
                    CooperativeLevelFlags.NonExclusive |
                    CooperativeLevelFlags.Background);

                joystickDevice.Acquire();
                joystickConected = true;

                // inicializo tabla de estados
                JoystickState state = joystickDevice.CurrentJoystickState;
                int len_jb = state.GetButtons().Length;
                Console.Out.Write(len_jb);
                previousJoystickState = new bool[len_jb];
                currentJoystickState = new bool[len_jb];
                for (int i = 0; i < len_jb; i++)
                {
                    currentJoystickState[i] = (state.GetButtons()[i] > 0);
                }
                previousJoystickState = currentJoystickState;
                Console.Out.Write("Joystick conectado\n");
            }
        }

        //retorna el estado de un boton
        public bool joy_button_down(XB360Digital button_id)
        {
            if (!joystickConected)
            {
                return false;
            }
            int buttonIntValue = (int)button_id;
            return currentJoystickState[buttonIntValue];
        }

        /// <summary>
        ///     Informa si un boton se dejo de presionar
        /// </summary>
        public bool joy_button_up(XB360Digital button_id)
        {
            var k = (int) button_id;
            return previousJoystickState[k] && !currentJoystickState[k];
        }

        /// <summary>
        ///     Informa si un boton se presiono y luego se libero
        /// </summary>
        public bool joy_button_pressed(XB360Digital button_id)
        {
            var k = (int) button_id;
            return !previousJoystickState[k] && currentJoystickState[k];
        }

        private int joy_applyDeadzone(int analogValue, int deadzoneValue)
        {
            if (analogValue > deadzoneValue)
            {
                return analogValue;
            }
            else
            {
                return 0;
            }
        }

        public int joy_analog(XB360Analog analogId)
        {
            if (!joystickConected)
            {
                return 0;
            }
            int analogValue = 0;
            switch (analogId)
            {
                case XB360Analog.STICK_LX:
                    analogValue = joy_applyDeadzone(joystickDevice.CurrentJoystickState.X, joystick_L_deadzone);
                    break;
                case XB360Analog.STICK_LY:
                    analogValue = joy_applyDeadzone(joystickDevice.CurrentJoystickState.Y, joystick_L_deadzone);
                    break;
                case XB360Analog.STICK_RX:
                    analogValue = joy_applyDeadzone(joystickDevice.CurrentJoystickState.Rx, joystick_R_deadzone);
                    break;
                case XB360Analog.STICK_RY:
                    analogValue = joy_applyDeadzone(joystickDevice.CurrentJoystickState.Ry, joystick_R_deadzone);
                    break;
                case XB360Analog.TRIGGER_L:
                    analogValue = joy_applyDeadzone(Math.Abs(joystickDevice.CurrentJoystickState.Z), joystick_TRIGGER_deadzone);
                    break;
                case XB360Analog.TRIGGER_R:
                    analogValue = joy_applyDeadzone(joystickDevice.CurrentJoystickState.Z, joystick_TRIGGER_deadzone);
                    break;
            }
            return analogValue;
        }

        /// <summary>
        ///     Filter the relative mouse movement based on a weighted sum of the mouse
        ///     movement from previous frames to ensure that the mouse movement this
        ///     frame is smooth.
        /// </summary>
        private void performMouseFiltering(int x, int y)
        {
            for (var i = historyBuffer.Length - 1; i > 0; --i)
            {
                historyBuffer[i].X = historyBuffer[i - 1].X;
                historyBuffer[i].Y = historyBuffer[i - 1].Y;
            }

            historyBuffer[0].X = x;
            historyBuffer[0].Y = y;

            var averageX = 0.0f;
            var averageY = 0.0f;
            var averageTotal = 0.0f;
            var currentWeight = 1.0f;

            for (var i = 0; i < historyBuffer.Length; i++)
            {
                averageX += historyBuffer[i].X * currentWeight;
                averageY += historyBuffer[i].Y * currentWeight;
                averageTotal += 1.0f * currentWeight;
                currentWeight *= WeightModifier;
            }

            XposRelative = averageX / averageTotal;
            YposRelative = averageY / averageTotal;
        }

        /// <summary>
        ///     Average the mouse movement across a couple of frames to smooth out mouse movement.
        /// </summary>
        private void performMouseSmoothing(float x, float y)
        {
            mouseMovement[mouseIndex].X = x;
            mouseMovement[mouseIndex].Y = y;

            XposRelative = (mouseMovement[0].X + mouseMovement[1].X) * 0.05f;
            YposRelative = (mouseMovement[0].Y + mouseMovement[1].Y) * 0.05f;

            mouseIndex ^= 1;
            mouseMovement[mouseIndex].X = 0.0f;
            mouseMovement[mouseIndex].Y = 0.0f;
        }

        /// <summary>
        ///     Informa si una tecla se encuentra presionada
        /// </summary>
        public bool keyDown(Key key)
        {
            if (!mouseInside) return false;

            var k = (int)key - 1;
            return currentkeyboardState[k];
        }

        /// <summary>
        ///     Informa si una tecla se dejo de presionar
        /// </summary>
        public bool keyUp(Key key)
        {
            if (!mouseInside) return false;

            var k = (int)key - 1;
            return previouskeyboardState[k] && !currentkeyboardState[k];
        }

        /// <summary>
        ///     Informa si una tecla se presiono y luego se libero
        /// </summary>
        public bool keyPressed(Key key)
        {
            if (!mouseInside) return false;

            var k = (int)key - 1;
            return !previouskeyboardState[k] && currentkeyboardState[k];
        }

        /// <summary>
        ///     Informa si un boton del mouse se encuentra presionado
        /// </summary>
        public bool buttonDown(MouseButtons button)
        {
            if (!mouseInside) return false;

            return currentMouseButtonsState[(int)button];
        }

        /// <summary>
        ///     Informa si un boton del mouse se dejo de presionar
        /// </summary>
        public bool buttonUp(MouseButtons button)
        {
            if (!mouseInside) return false;

            var b = (int)button;
            return previousMouseButtonsState[b] && !currentMouseButtonsState[b];
        }

        /// <summary>
        ///     Informa si un boton del mouse se presiono y luego se libero
        /// </summary>
        public bool buttonPressed(MouseButtons button)
        {
            if (!mouseInside) return false;

            var b = (int)button;
            return !previousMouseButtonsState[b] && currentMouseButtonsState[b];
        }

        #region Getters y Setters

        /// <summary>
        ///     Habilitar Mouse Smooth
        /// </summary>
        public bool EnableMouseSmooth { get; set; }

        /// <summary>
        ///     Influencia para filtrar el movimiento del mouse
        /// </summary>
        public float WeightModifier { get; set; }

        /// <summary>
        ///     Desplazamiento relativo de X del mouse
        /// </summary>
        public float XposRelative { get; private set; }

        /// <summary>
        ///     Desplazamiento relativo de Y del mouse
        /// </summary>
        public float YposRelative { get; private set; }

        /// <summary>
        ///     Posicion absoluta de X del mouse
        /// </summary>
        public float Xpos
        {
            get { return mouseX; }
        }

        /// <summary>
        ///     Posicion absoluta de Y del mouse
        /// </summary>
        public float Ypos
        {
            get { return mouseY; }
        }

        /// <summary>
        ///     Rueda del Mouse
        /// </summary>
        public float WheelPos { get; private set; }

        #endregion Getters y Setters
    }
}
