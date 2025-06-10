using System;
using System.Collections.Generic;
using System.Linq;

namespace _2048
{
    public class Game2048
    {
        public struct TileMove
        {
            public int FromRow, FromCol, ToRow, ToCol, Value;
            public bool Merged;
        }

        private int[,] board;
        private Random random;
        private int score;

        public Game2048()
        {
            board = new int[4, 4];
            random = new Random();
            score = 0;
            AddRandomTile();
            AddRandomTile();
        }

        public int[,] GetBoard() => board;
        public int GetScore() => score;

        // Returns the list of tile moves for animation
        public List<TileMove> MoveWithTracking(Direction direction)
        {
            bool boardChanged = false;
            int[,] original = (int[,])board.Clone();
            List<TileMove> moves = new List<TileMove>();

            for (int i = 0; i < 4; i++)
            {
                int[] line = new int[4];
                int[] fromRow = new int[4];
                int[] fromCol = new int[4];
                for (int j = 0; j < 4; j++)
                {
                    switch (direction)
                    {
                        case Direction.Left:
                            line[j] = board[i, j];
                            fromRow[j] = i;
                            fromCol[j] = j;
                            break;
                        case Direction.Right:
                            line[j] = board[i, 3 - j];
                            fromRow[j] = i;
                            fromCol[j] = 3 - j;
                            break;
                        case Direction.Up:
                            line[j] = board[j, i];
                            fromRow[j] = j;
                            fromCol[j] = i;
                            break;
                        case Direction.Down:
                            line[j] = board[3 - j, i];
                            fromRow[j] = 3 - j;
                            fromCol[j] = i;
                            break;
                    }
                }

                int[] merged = MergeLine(line, out int gainedScore);
                score += gainedScore;

                // Track moves
                for (int j = 0; j < 4; j++)
                {
                    int toRow = 0, toCol = 0;
                    switch (direction)
                    {
                        case Direction.Left:
                            toRow = i; toCol = j; break;
                        case Direction.Right:
                            toRow = i; toCol = 3 - j; break;
                        case Direction.Up:
                            toRow = j; toCol = i; break;
                        case Direction.Down:
                            toRow = 3 - j; toCol = i; break;
                    }
                    if (merged[j] != 0 && (fromRow[j] != toRow || fromCol[j] != toCol || merged[j] != line[j]))
                    {
                        moves.Add(new TileMove
                        {
                            FromRow = fromRow[j],
                            FromCol = fromCol[j],
                            ToRow = toRow,
                            ToCol = toCol,
                            Value = merged[j],
                            Merged = (merged[j] != line[j])
                        });
                    }
                }

                for (int j = 0; j < 4; j++)
                {
                    switch (direction)
                    {
                        case Direction.Left:
                            board[i, j] = merged[j];
                            break;
                        case Direction.Right:
                            board[i, 3 - j] = merged[j];
                            break;
                        case Direction.Up:
                            board[j, i] = merged[j];
                            break;
                        case Direction.Down:
                            board[3 - j, i] = merged[j];
                            break;
                    }
                }
            }

            // Check if the board changed
            for (int r = 0; r < 4; r++)
                for (int c = 0; c < 4; c++)
                    if (board[r, c] != original[r, c])
                        boardChanged = true;

            if (boardChanged)
                AddRandomTile();

            return boardChanged ? moves : new List<TileMove>();
        }

        // Backward compatibility for existing code
        public bool Move(Direction direction)
        {
            return MoveWithTracking(direction).Count > 0;
        }

        // Helper to merge a line for 2048
        private int[] MergeLine(int[] oldLine, out int gainedScore)
        {
            List<int> tiles = oldLine.Where(x => x != 0).ToList();
            List<int> merged = new List<int>();
            gainedScore = 0;
            int skip = -1;
            for (int i = 0; i < tiles.Count; i++)
            {
                if (i == skip)
                    continue;
                if (i + 1 < tiles.Count && tiles[i] == tiles[i + 1])
                {
                    int newValue = tiles[i] * 2;
                    merged.Add(newValue);
                    gainedScore += newValue;
                    skip = i + 1;
                }
                else
                {
                    merged.Add(tiles[i]);
                }
            }
            while (merged.Count < 4)
                merged.Add(0);
            return merged.ToArray();
        }

        public void AddRandomTile()
        {
            List<(int, int)> emptyCells = new List<(int, int)>();
            for (int row = 0; row < 4; row++)
            {
                for (int col = 0; col < 4; col++)
                {
                    if (board[row, col] == 0)
                    {
                        emptyCells.Add((row, col));
                    }
                }
            }

            if (emptyCells.Count > 0)
            {
                var (row, col) = emptyCells[random.Next(emptyCells.Count)];
                board[row, col] = random.Next(10) < 9 ? 2 : 4;
            }
        }

        public bool HasLost()
        {
            // Check if any empty spaces
            if (board.Cast<int>().Any(x => x == 0))
                return false;

            // Check for possible merges
            for (int row = 0; row < 4; row++)
            {
                for (int col = 0; col < 4; col++)
                {
                    if ((col < 3 && board[row, col] == board[row, col + 1]) ||
                        (row < 3 && board[row, col] == board[row + 1, col]))
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        public bool HasWon()
        {
            return board.Cast<int>().Any(x => x == 2048);
        }

        public enum Direction { Up, Down, Left, Right }
    }
}
