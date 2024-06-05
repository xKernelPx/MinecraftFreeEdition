using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Runtime.InteropServices;
using CubesWPF;
using System.Net.NetworkInformation;

namespace CursedWork
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        [DllImport("User32.dll")]
        private static extern bool SetCursorPos(int X, int Y);
        
        double circle_X_Z = 0;
        double half_circle_Y = 0;

        double sinY = 0;
        double sinY_2 = 0;
        double cosX_Z = 0;
        double cosX_Z_2 = 0;
        double sinX_Z = 0;
        double sinX_Z_2 = 0;
        double _X_Z_mult = 0;

        double sensitivity_horiz = 0.005;
        double sensitivity_vert = 0.003;

        bool mouse_lock = true;

        Point center;
        Point real_center;

        CubeModel[] cubes;
        int inventory_index = 0;

        int[] vertex_indexes_Y = new int[] { 20 };
        int[] vertex_indexes_y = new int[] { 16, 19 };
        int[] vertex_indexes_X = new int[] { 12, 15 };
        int[] vertex_indexes_x = new int[] { 9, 10 };
        int[] vertex_indexes_Z = new int[] { 4, 7 };
        int[] vertex_indexes_z = new int[] { 1, 2 };

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_MouseMove(object sender, MouseEventArgs e)
        {
            if (!mouse_lock)
            {
                return;
            }

            Point mouse = e.GetPosition(this);
            Vector3D look = camera.LookDirection;

            circle_X_Z += (PointToScreen(mouse).X - real_center.X) * sensitivity_horiz;
            circle_X_Z = circle_X_Z >= 0 ? circle_X_Z % (2 * Math.PI) : - (-circle_X_Z % (2 * Math.PI));

            half_circle_Y -= (PointToScreen(mouse).Y - real_center.Y) * sensitivity_vert;
            half_circle_Y = Math.Abs(half_circle_Y) > (0.49 * Math.PI) ? half_circle_Y >= 0 ? 0.49 * Math.PI : -0.49 * Math.PI : half_circle_Y;

            sinY = Math.Sin(half_circle_Y);
            _X_Z_mult = 1 - Math.Abs(sinY);
            sinY_2 = sinY * sinY >= 0 ? sinY : -sinY;
            cosX_Z = Math.Cos(circle_X_Z);
            cosX_Z_2 = cosX_Z * cosX_Z >= 0 ? cosX_Z : -cosX_Z;
            sinX_Z = Math.Sin(circle_X_Z);
            sinX_Z_2 = sinX_Z * sinX_Z >= 0 ? sinX_Z : -sinX_Z;
            look.X = cosX_Z * _X_Z_mult;
            look.Z = sinX_Z * _X_Z_mult;
            look.Y = sinY;

            SetCursor((int)center.X, (int)center.Y);

            camera.LookDirection = look;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            cubes = new CubeModel[] { CubeModels.Dirt, CubeModels.Cobblestone, CubeModels.Bricks };
            center = new Point((int)viewport.Width / 2, (int)viewport.Height / 2);
            Cube cub;
            for (int i = -25; i < 25; i++)
            {
                for (int j = -25; j < 25; j++)
                {
                    cub = new Cube(viewport, CubeModels.Dirt);
                    cub.Instantiate(new Point3D(i, 0, j));
                }
            }
            return;
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            Point3D pos = camera.Position;
            switch (e.Key)
            {
                case Key.W:
                    pos += camera.LookDirection;
                    break;
                case Key.S:
                    pos -= camera.LookDirection;
                    break;
                case Key.A:
                    pos.X += sinX_Z_2;
                    pos.Z -= cosX_Z_2;
                    break;
                case Key.D:
                    pos.X -= sinX_Z_2;
                    pos.Z += cosX_Z_2;
                    break;
                case Key.Escape:
                    mouse_lock = false;
                    Cursor = Cursors.Arrow;
                    break;
                case Key.F11:
                    if (WindowStyle == WindowStyle.ToolWindow)
                    {
                        WindowState = WindowState.Maximized;
                        WindowStyle = WindowStyle.None;
                    }
                    else
                    {
                        WindowState = WindowState.Normal;
                        WindowStyle = WindowStyle.ToolWindow;
                    }
                    break;
                default:
                    break;
            }
            camera.Position = pos;
        }

        private void SetCursor(int x, int y)
        {
            real_center = PointToScreen(new Point(x, y));
            SetCursorPos((int)real_center.X, (int)real_center.Y);
        }

        private void mainWindow_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            center = new Point((int)viewport.ActualWidth / 2, (int)viewport.ActualHeight / 2);
        }

        private void mainWindow_MouseEnter(object sender, MouseEventArgs e)
        {
            center = new Point((int)viewport.ActualWidth / 2, (int)viewport.ActualHeight / 2);
            mouse_lock = true;
            Cursor = Cursors.None;
        }

        private void mainWindow_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            PointHitTestParameters raycast_params = new PointHitTestParameters(center);
            VisualTreeHelper.HitTest(viewport, null, ResultCallback, raycast_params);
        }

        private void mainWindow_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            inventory_index = ((inventory_index + (e.Delta > 0 ? 1 : -1)) + cubes.Length) % cubes.Length;
        }

        public HitTestResultBehavior ResultCallback(HitTestResult result)
        {
            RayHitTestResult ray_result = result as RayHitTestResult;
            if (ray_result != null) 
            {
                RayMeshGeometry3DHitTestResult mesh_result = ray_result as RayMeshGeometry3DHitTestResult;
                if (mesh_result != null) 
                {
                    double distance = mesh_result.DistanceToRayOrigin;
                    if (distance > 8 || distance <= 1)
                    {
                        return HitTestResultBehavior.Stop;
                    }

                    Point3D spawn_point = new Point3D(mesh_result.PointHit.X, mesh_result.PointHit.Y, mesh_result.PointHit.Z);

                    int index = mesh_result.VertexIndex1;
                    bool detect = true;

                    if (vertex_indexes_X.Contains(index))
                    {
                        spawn_point.X += 0.5;
                    }
                    else if (vertex_indexes_x.Contains(index))
                    {
                        spawn_point.X -= 0.5;
                    }
                    else if (vertex_indexes_Y.Contains(index))
                    {
                        spawn_point.Y += 0.5;
                    }
                    else if (vertex_indexes_y.Contains(index))
                    {
                        spawn_point.Y -= 0.5;
                    }
                    else if (vertex_indexes_Z.Contains(index))
                    {
                        spawn_point.Z += 0.5;
                    }
                    else if (vertex_indexes_z.Contains(index))
                    {
                        spawn_point.Z -= 0.5;
                    }
                    else
                    {
                        detect = false;
                    }

                    if (detect)
                    {
                        Cube new_cube;
                        new_cube = new Cube(viewport, cubes[inventory_index]);
                        new_cube.Instantiate(new Point3D(Math.Round(spawn_point.X), Math.Round(spawn_point.Y), Math.Round(spawn_point.Z)));
                    }
                    

                }
            }
            return HitTestResultBehavior.Stop;
        }

        private void mainWindow_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            PointHitTestParameters raycast_params = new PointHitTestParameters(center);
            VisualTreeHelper.HitTest(viewport, null, ResultCallbackDel, raycast_params);
        }

        public HitTestResultBehavior ResultCallbackDel(HitTestResult result)
        {
            RayHitTestResult ray_result = result as RayHitTestResult;
            if (ray_result != null)
            {
                RayMeshGeometry3DHitTestResult mesh_result = ray_result as RayMeshGeometry3DHitTestResult;
                if (mesh_result != null)
                {
                    if (mesh_result.DistanceToRayOrigin > 8)
                    {
                        return HitTestResultBehavior.Stop;
                    }
                    viewport.Children.Remove(mesh_result.VisualHit);
                }
            }
            return HitTestResultBehavior.Stop;
        }
    }
}