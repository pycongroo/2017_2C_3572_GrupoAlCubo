using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TGC.Group.Model
{
    public struct Point : IEquatable<Point>
    {
        public int X { get; private set; }
        public int Y { get; private set; }

        public Point(int x, int y)
        {
            this.X = x;
            this.Y = y;
        }

        public bool Equals(Point other)
        {
            return X == other.X && Y == other.Y;
        }
    }

    public static class Extensions
    {
        public static IEnumerable<T> Shuffle<T>(this IEnumerable<T> source, Random rng)
        {
            var e = source.ToArray();
            for (var i = e.Length - 1; i >= 0; i--)
            {
                var swapIndex = rng.Next(i + 1);
                yield return e[swapIndex];
                e[swapIndex] = e[i];
            }
        }

        public static CellState OppositeWall(this CellState orig)
        {
            return (CellState)(((int)orig >> 2) | ((int)orig << 2)) & CellState.Initial;
        }

        public static bool HasFlag(this CellState cs, CellState flag)
        {
            return ((int)cs & (int)flag) != 0;
        }
    }

    [Flags]
    public enum CellState
    {
        Top = 1,
        Right = 2,
        Bottom = 4,
        Left = 8,
        Visited = 128,
        Initial = Top | Right | Bottom | Left,
    }

    public struct RemoveWallAction
    {
        public Point Neighbour;
        public CellState Wall;
    }

    public class Maze
    {
        private readonly CellState[,] _cells;
        private readonly int _width;
        private readonly int _height;
        private readonly Random _rng;
        public static readonly int WALL = 2;
        public static readonly int PATH = 1;

        public int Width => _width;
        public int Height => _height;

        public Maze(int width, int height)
        {
            _width = width;
            _height = height;
            _cells = new CellState[width, height];
            for (var x = 0; x < width; x++)
                for (var y = 0; y < height; y++)
                    _cells[x, y] = CellState.Initial;
            _rng = new Random();
            VisitCell(_rng.Next(width), _rng.Next(height));
        }

        public CellState this[int x, int y]
        {
            get { return _cells[x, y]; }
            set { _cells[x, y] = value; }
        }

        public IEnumerable<RemoveWallAction> GetNeighbours(Point p)
        {
            if (p.X > 0) yield return new RemoveWallAction { Neighbour = new Point(p.X - 1, p.Y), Wall = CellState.Left };
            if (p.Y > 0) yield return new RemoveWallAction { Neighbour = new Point(p.X, p.Y - 1), Wall = CellState.Top };
            if (p.X < _width - 1) yield return new RemoveWallAction { Neighbour = new Point(p.X + 1, p.Y), Wall = CellState.Right };
            if (p.Y < _height - 1) yield return new RemoveWallAction { Neighbour = new Point(p.X, p.Y + 1), Wall = CellState.Bottom };
        }

        public void VisitCell(int x, int y)
        {
            this[x, y] |= CellState.Visited;
            foreach (var p in GetNeighbours(new Point(x, y)).Shuffle(_rng).Where(z => !(this[z.Neighbour.X, z.Neighbour.Y].HasFlag(CellState.Visited))))
            {
                this[x, y] -= p.Wall;
                this[p.Neighbour.X, p.Neighbour.Y] -= p.Wall.OppositeWall();
                VisitCell(p.Neighbour.X, p.Neighbour.Y);
            }
        }


        public void Display()
        {
            var firstLine = string.Empty;
            for (var y = 0; y < _height; y++)
            {
                var sbTop = new StringBuilder();
                var sbMid = new StringBuilder();
                for (var x = 0; x < _width; x++)
                {
                    sbTop.Append(this[x, y].HasFlag(CellState.Top) ? "+--" : "+  ");
                    sbMid.Append(this[x, y].HasFlag(CellState.Left) ? "|  " : "   ");
                }
                if (firstLine == string.Empty)
                    firstLine = sbTop.ToString();

                Console.WriteLine(sbTop + "+");
                Console.WriteLine(sbMid + "|");
                Console.WriteLine(sbMid + "|");
            }
            Console.WriteLine(firstLine);
        }

        public int[,] ToMatrix()
        {
            int matrix_width = _width * 2 + 1;
            int matrix_height = _height * 2 + 1;
            int[,] matrix = new int[matrix_width, matrix_height];
            for (int matrix_x = 0; matrix_x < matrix_width; matrix_x++)
            {
                for (int matrix_y = 0; matrix_y < matrix_height; matrix_y++)
                {
                    if (matrix_x == matrix_width - 1 || matrix_y == matrix_height - 1)
                    {
                        matrix[matrix_x, matrix_y] = WALL;
                    }
                }
            }


            for (var y = 0; y < _height; y++)
            {
                var raw1 = y * 2;
                var raw2 = y * 2 + 1;
                for (var x = 0; x < _width; x++)
                {
                    var col1 = x * 2;
                    var col2 = x * 2 + 1;

                    if (this[x, y].HasFlag(CellState.Top))
                    {
                        matrix[raw1, col1] = WALL;
                        matrix[raw1, col2] = WALL;
                    }
                    else
                    {
                        matrix[raw1, col1] = WALL;
                        matrix[raw1, col2] = PATH;
                    }
                    if (this[x, y].HasFlag(CellState.Left))
                    {
                        matrix[raw2, col1] = WALL;
                        matrix[raw2, col2] = PATH;
                    }
                    else
                    {
                        matrix[raw2, col1] = PATH;
                        matrix[raw2, col2] = PATH;
                    }
                }
            }
            return matrix;
        }

        public List<Point> FindPath(Point from, Point to)
        {
            return new PathFinder(ToMatrix(), ToMatrixPoint(from), ToMatrixPoint(to)).Solution();
        }

        private static Point ToMazePoint(Point matrixPoint)
        {
            return new Point(matrixPoint.X / 2, matrixPoint.Y / 2);
        }

        private static Point ToMatrixPoint(Point mazePoint)
        {
            return new Point(mazePoint.X * 2 + 1, mazePoint.Y * 2 + 1);
        }


        public class PathFinder
        {
            private int[,] maze; // Maze (1 = path, 2 = wall)
            private Stack<Point> path = new Stack<Point>();
            private Point start; // Starting X and Y values of maze
            private Point end;     // Ending X and Y values of maze

            public PathFinder(int[,] maze, Point from, Point to)
            {
                this.maze = maze;
                this.start = from;
                this.end = to;
            }

            public List<Point> Solution()
            {
                List<Point> solution = new List<Point>();
                if (SolveMaze())
                {
                    while (path.Count > 0)
                    {
                        Point mazePoint = ToMazePoint(path.Pop());
                        if (!solution.Contains(mazePoint))
                        {
                            solution.Add(mazePoint);
                        }
                    }
                }
                return solution;
            }

            public bool SolveMaze()
            {
                bool[,] wasHere = new bool[maze.GetLength(0), maze.GetLength(1)];
                // Maze (1 = path, 2 = wall)
                for (int row = 0; row < maze.GetLength(0); row++)
                    // Sets boolean Arrays to default values
                    for (int col = 0; col < maze.GetLength(1); col++)
                    {
                        wasHere[row, col] = false;
                    }
                return recursiveSolve(start.X, start.Y, wasHere);
                // Will leave you with a boolean array (correctPath) 
                // with the path indicated by true values.
                // If b is false, there is no solution to the maze
            }

            public bool recursiveSolve(int x, int y, bool[,] wasHere)
            {
                if (x == end.X && y == end.Y)
                {
                    path.Push(new Point(x, y));
                    return true; // If you reached the end
                }
                if (maze[x, y] == Maze.WALL || wasHere[x, y]) return false;
                // If you are on a wall or already were here
                wasHere[x, y] = true;
                if (x != 0) // Checks if not on left edge
                    if (recursiveSolve(x - 1, y, wasHere))
                    { // Recalls method one to the left
                        path.Push(new Point(x, y));
                        return true;
                    }
                if (x != maze.GetLength(0) - 1) // Checks if not on right edge
                    if (recursiveSolve(x + 1, y, wasHere))
                    { // Recalls method one to the right
                        path.Push(new Point(x, y));
                        return true;
                    }
                if (y != 0)  // Checks if not on top edge
                    if (recursiveSolve(x, y - 1, wasHere))
                    { // Recalls method one up
                        path.Push(new Point(x, y));
                        return true;
                    }
                if (y != maze.GetLength(1) - 1) // Checks if not on bottom edge
                    if (recursiveSolve(x, y + 1, wasHere))
                    { // Recalls method one down
                        path.Push(new Point(x, y));
                        return true;
                    }
                return false;
            }
        }

    }
}
