﻿using System;
using System.Linq;

namespace ChessGame
{
    public enum GameState : byte
    {
        Stalemate,
        Checkmate,
        Check,
        NotStarted,
        Started,
        DeadPosition
    }

    public abstract class Gamemode
    {
        public event Action<GameState> onGameStateUpdated;
        public event Action onTurnChanged;

        /// <summary>
        /// Null if no winner has been selected.
        /// </summary>
        public Player Winner;
        public readonly Player PlayerWhite;
        public readonly Player PlayerBlack;

        public Gamemode(Player playerWhite, Player playerBlack)
        {
            PlayerWhite = playerWhite;
            PlayerBlack = playerBlack;
        }

        // TODO: maybe implement classic chess by standard (make this virtual).
        public abstract Chessboard GenerateBoard();

        /// <summary>
        /// Validates a move for a given position. Maybe merge with <c>MakeMove(Move)</c>
        /// </summary>
        /// <param name="move"></param>
        /// <param name="board"></param>
        /// <returns></returns>
        public virtual bool ValidateMove(Move move, Chessboard board)
        {
            // if move is outside board, then it's invalid.
            foreach (var singleMove in move.Moves)
            {
                if (singleMove.Destination is null)
                {
                    continue;
                }

                if (!board.InsideBoard(singleMove.Destination.Value))
                {
                    return false;
                }
            }

            // if move puts king in check — it's invalid.
            Chessboard boardSimulation = new Chessboard(board);
            boardSimulation.ExecuteMove(move);

            // king is in check, move is invalid
            if (boardSimulation.IsKingInCheck(board.CurrentTeamTurn))
            {
                return false;
            }

            // move is valid
            return true;
        }

        /// <summary>
        /// Updates gamestate, e.g. checks for mate/checkmate or stalemate. Run at end of turn.
        /// </summary>
        /// <returns>Whether the gamestate has updated.</returns>
        /// <param name="board"></param>
        public virtual bool UpdateGameState(Chessboard board)
        {
            GameState previousState = board.CurrentState;

            if (previousState == GameState.NotStarted)
            {
                return false;
            }

            // check for whether king is in check.
            if (board.IsKingInCheck(board.CurrentTeamTurn))
            {
                board.CurrentState = GameState.Check;
            }
            else
            {
                board.CurrentState = GameState.Started;
            }

            if (board.Pieces.Count <= 3)
            {
                bool draw = true;
                foreach (var item in board.Pieces)
                {
                    if (item.Value is Pieces.Queen || item.Value is Pieces.Rook || item.Value is Pieces.Pawn)
                    {
                        draw = false;
                    }
                }

                if (draw)
                {
                    board.CurrentState = GameState.DeadPosition;
                    return true;
                }
            }

            // no more legal moves, game is over either by stalemate or checkmate.
            if (!board.GetMoves(board.CurrentTeamTurn).Any())
            {
                // checkmate
                if (board.CurrentState == GameState.Check)
                {
                    Winner = board.CurrentTeamTurn == TeamColor.White ? PlayerBlack : PlayerWhite;
                    board.CurrentState = GameState.Checkmate;
                }
                else
                {
                    board.CurrentState = GameState.Stalemate;
                }
            }
            else if(board.CurrentState != GameState.Check)
            {
                board.CurrentState = GameState.Started;
            }

            return previousState != board.CurrentState;
        }

        /// <summary>
        /// Can be overridden to have custom turn logic.
        /// </summary>
        /// <param name="board"></param>
        /// <returns>False if game has ended.</returns>
        public virtual bool StartTurn(Chessboard board)
        {
            if (UpdateGameState(board))
            {
                onGameStateUpdated?.Invoke(board.CurrentState);

                switch (board.CurrentState)
                {
                    case GameState.Stalemate:
                    case GameState.Checkmate:
                        return false;
                }
            }

            onTurnChanged?.Invoke();
            return true;
        }
    }
}
