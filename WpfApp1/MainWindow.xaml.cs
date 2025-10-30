using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace WpfApp1
{
    public partial class MainWindow : Window
    {
        private const int PillarWidth = 18;
        private const int DiskHeight = 24;
        private const int BaseLineY = 450;
        private readonly List<Stack<Rectangle>> _towers = new List<Stack<Rectangle>>();
        private int _moveCount;
        private DispatcherTimer _animationTimer;
        private Rectangle _currentDisk;
        private double _targetX, _targetY, _startX, _startY;
        private int _animationStep;
        private readonly int _animationSteps = 30;
        private Queue<Action> _moveQueue = new Queue<Action>();
        private bool _isAnimating;
        private int _animationPhase; 
        private int _fromIndex, _toIndex;

        public MainWindow()
        {
            InitializeComponent();
            InitializeTowers();
            SetupGame(4);
            InitializeAnimationTimer();
        }

        private void InitializeAnimationTimer()
        {
            _animationTimer = new DispatcherTimer();
            _animationTimer.Interval = TimeSpan.FromMilliseconds(1);
            _animationTimer.Tick += AnimationTimer_Tick;
        }

        private void InitializeTowers()
        {
            for (int i = 0; i < 3; i++)
            {
                _towers.Add(new Stack<Rectangle>());
            }
        }

        private void OnStartButtonClick(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(DiskQuantityTextBox.Text, out int count) && count >= 1 && count <= 6)
            {
                RunSolution(count);
            }
            else
            {
                MessageBox.Show("Пожалуйста, выберите количество дисков от 1 до 6");
            }
        }

        private void RunSolution(int disks)
        {
            StartButton.IsEnabled = false;
            ResetButton.IsEnabled = false;

            SetupGame(disks);
            _moveCount = 0;
            UpdateStepCounter();

            _moveQueue.Clear();
            BuildMoveQueue(disks, 0, 2, 1);
            ProcessNextMove();
        }

        private void BuildMoveQueue(int n, int from, int to, int via)
        {
            if (n == 0) return;
            BuildMoveQueue(n - 1, from, via, to);
            _moveQueue.Enqueue(() => MoveDisk(from, to));
            BuildMoveQueue(n - 1, via, to, from);
        }

        private void ProcessNextMove()
        {
            if (_moveQueue.Count > 0 && !_isAnimating)
            {
                var move = _moveQueue.Dequeue();
                move();
            }
            else if (_moveQueue.Count == 0 && !_isAnimating)
            {
                StartButton.IsEnabled = true;
                ResetButton.IsEnabled = true;
            }
        }

        private void SetupGame(int diskCount)
        {
            GameCanvas.Children.Clear();

            for (int i = 0; i < 3; i++)
            {
                var tower = CreateTower(i);
                GameCanvas.Children.Add(tower);
            }

            var platform = new Rectangle
            {
                Width = 750,
                Height = 25,
                Fill = Brushes.Peru,
                Stroke = Brushes.Sienna,
                StrokeThickness = 2
            };
            Canvas.SetLeft(platform, 75);
            Canvas.SetTop(platform, BaseLineY);
            GameCanvas.Children.Add(platform);

            _towers.ForEach(t => t.Clear());
            for (int i = diskCount; i > 0; i--)
            {
                var disk = CreateDisk(i);
                PositionDisk(disk, 0, _towers[0].Count);
                _towers[0].Push(disk);
                GameCanvas.Children.Add(disk);
            }
        }

        private Rectangle CreateTower(int index)
        {
            var tower = new Rectangle
            {
                Width = PillarWidth,
                Height = 220,
                Fill = Brushes.Chocolate,
                Stroke = Brushes.SaddleBrown,
                StrokeThickness = 2
            };
            Canvas.SetLeft(tower, GetTowerX(index) - PillarWidth / 2);
            Canvas.SetTop(tower, BaseLineY - 220);
            return tower;
        }

        private Rectangle CreateDisk(int size)
        {
            int width = 30 + size * 25;
            return new Rectangle
            {
                Width = width,
                Height = DiskHeight,
                Fill = GetDiskColor(size),
                Stroke = Brushes.Black,
                StrokeThickness = 1,
                RadiusX = 8,
                RadiusY = 8
            };
        }

        private void PositionDisk(Rectangle disk, int towerIndex, int level)
        {
            double x = GetTowerX(towerIndex) - disk.Width / 2;
            double y = BaseLineY - (level + 1) * DiskHeight;
            Canvas.SetLeft(disk, x);
            Canvas.SetTop(disk, y);
        }

        private double GetTowerX(int index)
        {
            return 150 + index * 300;
        }

        private SolidColorBrush GetDiskColor(int size)
        {
            Color[] colors = {
                Colors.Crimson, Colors.DarkOrange, Colors.Yellow,
                Colors.ForestGreen, Colors.DodgerBlue, Colors.DarkViolet,
            };
            return new SolidColorBrush(colors[size % colors.Length]);
        }

        private void MoveDisk(int fromIndex, int toIndex)
        {
            if (_towers[fromIndex].Count == 0) return;

            _fromIndex = fromIndex;
            _toIndex = toIndex;
            _currentDisk = _towers[fromIndex].Pop();
            _startX = Canvas.GetLeft(_currentDisk);
            _startY = Canvas.GetTop(_currentDisk);

            _animationPhase = 0;
            _targetX = _startX;
            _targetY = BaseLineY - 300;

            _isAnimating = true;
            _animationStep = 0;
            _animationTimer.Start();
        }

        private void AnimationTimer_Tick(object sender, EventArgs e)
        {
            if (_animationStep < _animationSteps)
            {
                double progress = (double)_animationStep / _animationSteps;
                double currentX = _startX + (_targetX - _startX) * progress;
                double currentY = _startY + (_targetY - _startY) * progress;

                Canvas.SetLeft(_currentDisk, currentX);
                Canvas.SetTop(_currentDisk, currentY);

                _animationStep++;
            }
            else
            {
                Canvas.SetLeft(_currentDisk, _targetX);
                Canvas.SetTop(_currentDisk, _targetY);

                _animationPhase++;

                if (_animationPhase == 1)
                {
                    _startX = _targetX;
                    _startY = _targetY;
                    _targetX = GetTowerX(_toIndex) - _currentDisk.Width / 2;
                    _targetY = _startY; 
                    _animationStep = 0;
                }
                else if (_animationPhase == 2)
                {
                    _startX = _targetX;
                    _startY = _targetY;
                    _targetY = BaseLineY - (_towers[_toIndex].Count + 1) * DiskHeight;
                    _animationStep = 0;
                }
                else
                {
                    _animationTimer.Stop();
                    _towers[_toIndex].Push(_currentDisk);
                    _moveCount++;
                    UpdateStepCounter();
                    _isAnimating = false;
                    ProcessNextMove();
                }
            }
        }

        private void UpdateStepCounter()
        {
            StepCounter.Text = $"Количество перемещений: {_moveCount}";
        }

        private void OnResetButtonClick(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(DiskQuantityTextBox.Text, out int count) && count >= 1 && count <= 6)
            {
                _animationTimer.Stop();
                _isAnimating = false;
                _moveQueue.Clear();
                SetupGame(count);
                StartButton.IsEnabled = true;
                ResetButton.IsEnabled = true;
            }
        }
    }
}