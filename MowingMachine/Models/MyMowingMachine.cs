﻿using System;
using System.Collections.Generic;
using System.Linq;
using MowingMachine.Services;

namespace MowingMachine.Models
{
    public class MyMowingMachine
    {
        // These are going to be all the coordinates we go, to mow the grass at that coordinate.
        private readonly List<Field> _discoveredFields = new();

        // The key is the offset (the visited field) and the value is the list of fields that can be visited from that field.
        // These fields in the list haven't been visited yet.
        private readonly List<KeyValuePair<MowingStep, List<Offset>>> _moves = new();

        // Contains information about the map
        private readonly MapManager _mapManager;

        // This field preserves the most recent field the mowing machine was on. Initially it will be the charging station.
        private FieldType _currentFieldType = FieldType.ChargingStation;
        
        private MoveDirection _currentFacingDirection = MoveDirection.Left;
        private MoveDirection _currentDirection = MoveDirection.Left;
        private Field _currentField;

        private bool _isGoingToChargingStation;
        private bool _isMowing;

        public MyMowingMachine(MapManager mapManager)
        {
            _mapManager = mapManager;
            
            // Add initial fields
            _currentField = GetField(_mapManager.GetFieldsOfView(), new Offset(0, 0), FieldType.ChargingStation);
            _discoveredFields.Add(_currentField);
        }

        private Field GetField(FieldOfView fov, Offset offset, FieldType fieldType)
        {
            if (fieldType == FieldType.ChargingStation)
            {
                    
            }
            
            var offsetTop = offset.Add(0, 1);
            var offsetRight = offset.Add(1, 0);
            var offsetBottom = offset.Add(0, -1);
            var offsetLeft = offset.Add(-1, 0);
            
            var neighbors = new List<Field>
            {
                _discoveredFields.SingleOrDefault(f => f.Offset.CompareTo(offsetTop)) ?? new Field(fov.TopCasted, offsetTop),
                _discoveredFields.SingleOrDefault(f => f.Offset.CompareTo(offsetRight)) ?? new Field(fov.RightCasted, offsetRight),
                _discoveredFields.SingleOrDefault(f => f.Offset.CompareTo(offsetBottom)) ?? new Field(fov.BottomCasted, offsetBottom),
                _discoveredFields.SingleOrDefault(f => f.Offset.CompareTo(offsetLeft)) ?? new Field(fov.LeftCasted, offsetLeft),
            };
            
            return new Field(fieldType, offset, neighbors);
        }

        public bool PerformMove()
        {
            if (!_mapManager.Verify())
                return false;
            
            if (_discoveredFields.All(f => f.IsVisited))
            {
                Complete();
                return true;
            }
            
            if (_isGoingToChargingStation)
            {
                MoveToChargingStation();
                return false;
            }

            var calculatedSteps = CalculateNextMove();
            
            var needsToRefuelFirst = NeedsToRefuel(calculatedSteps);
            if (needsToRefuelFirst)
            {
                MoveToChargingStation();
                return false;
            }
            
            PerformStep(calculatedSteps);
            return false;
        }
        
        private static MowingStep CalculateStopExpense(MoveDirection direction, MoveDirection currentFacingDirection, FieldType currentFieldType)
        {
            var turns = new Queue<MoveDirection>();
            
            if (currentFacingDirection != direction)
                turns = CalculateTurn(currentFacingDirection, direction, turns);

            return new MowingStep(turns, direction, currentFieldType);
        }
        
        private Field Move(MowingStep step, Offset newOffset, FieldType? updatePrevFieldType = null)
        {
            NoteNextMove(step);
            _currentFieldType = _mapManager.MoveMowingMachine(step, updatePrevFieldType ?? _currentFieldType);

            var type = updatePrevFieldType ?? _currentFieldType;
            
            if (_discoveredFields.Any() && type == FieldType.ChargingStation)
                return GetField(_mapManager.GetFieldsOfView(), newOffset, FieldType.MowedLawn);
            return GetField(_mapManager.GetFieldsOfView(), newOffset, type);
        }

        private static Queue<MoveDirection> CalculateTurn(MoveDirection direction, MoveDirection finalDirection, Queue<MoveDirection> moves)
        {
            if (direction == finalDirection)
                return moves;

            int currentDir = (int) direction;
            int finalDir = (int) finalDirection;

            var x = Math.Min(Math.Abs(currentDir - finalDir), Math.Abs(finalDir - finalDir));

            if (x == 6)
            {
                switch (direction)
                {
                    case MoveDirection.Top:
                    case MoveDirection.Bottom:
                        direction = MoveDirection.Left;
                        moves.Enqueue(MoveDirection.Left);
                        break;
                    case MoveDirection.Right:
                    case MoveDirection.Left:
                        direction = MoveDirection.Top;
                        moves.Enqueue(MoveDirection.Top);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(direction), direction, null);
                }

                return CalculateTurn(direction, finalDirection, moves);
            }
            
            moves.Enqueue(finalDirection);
            return moves;
        }

        private void NoteNextMove(MowingStep step)
        {
            // Todo: Save the move in there giving data structure.
        }

        private void Complete()
        {
            // Todo: Maybe double check if all the grass was mowed.
            Console.WriteLine("Mowing complete!");
        }

        private bool NeedsToRefuel(List<MowingStep> steps)
        {
            // Todo: Check one step ahead if the fuel would be enough to go back to the charging station.
            return false;
        }

        private void PerformStep(List<MowingStep> steps)
        {
            // Todo: Continue here.
            
            // // Update values
            // _currentFacingDirection = direction.Value;
            // _currentField = Move(step, _discoveredFields.Last().Offset.Add(new Offset(direction.Value)),
            //     _currentFieldType is FieldType.Grass ? FieldType.MowedLawn : _currentFieldType);
            // // _currentField.IsVisited = true;
            // nextField.IsVisited = true;
            //
            // // Update neighbor fields
            // _discoveredFields.ForEach(f => f.UpdateFieldNeighbor(_currentField));
            // _discoveredFields.Add(_currentField);
        }

        private List<MowingStep> CalculateNextMove()
        {
            // Todo: Double check if the inversion is right.
            var successful = GetNextNeighborField(out var values);
            var (_, direction) = values;

            if (!successful)
            {
                var steps = new List<MowingStep>();
                var currentDirection = _currentFacingDirection;
                var currentlyStandFieldType = _currentFieldType;

                for (int i = _moves.Count - 1; i >= 0; i--)
                {
                    var (prevFieldStep, neighbors) = _moves[i];
                    
                    var step = CalculateStopExpense(prevFieldStep.MoveDirection.InvertDirection(), currentDirection, currentlyStandFieldType);
                    currentlyStandFieldType = prevFieldStep.FieldType;
                    steps.Add(step);

                    currentDirection = prevFieldStep.MoveDirection;
                    
                    if (neighbors.Any())
                    {
                        var finalDirection = MowingMachineService.TranslateOffsetToDirection(neighbors.First());
                        
                        var stepFinal = CalculateStopExpense(finalDirection.InvertDirection(), currentDirection, prevFieldStep.FieldType);
                        steps.Add(stepFinal);
                        
                        break;
                    }
                }

                return steps;
            }
            
            var nextStep = CalculateStopExpense(direction.InvertDirection(), _currentDirection, _currentFieldType);

            return new List<MowingStep> { nextStep };
            
            // var nextFieldToVisit = _discoveredFields.FindLast(f => !f.IsVisited);
            // // If there is no next field to visit, we are done.
            // // Todo: We can remove this if only the perform move method calls the function
            // if (nextFieldToVisit is null)
            //     return;
            
            
            
            // Get next field to move on
            // if (!IsNeighborField(nextFieldToVisit, out var direction))
            // {
            //     // If 
            // }

            bool GetNextNeighborField(out (Field, MoveDirection) result)
            {
                var fieldIndex = _currentField.NeighborFields?.FindIndex(f => !f.IsVisited && f.CanBeWalkedOn());
                result = (null, MoveDirection.Bottom);
                
                if (!fieldIndex.HasValue)
                    return false;

                result = (_currentField.NeighborFields[fieldIndex.Value], fieldIndex switch
                {
                    0 => MoveDirection.Top,
                    1 => MoveDirection.Right,
                    2 => MoveDirection.Bottom,
                    3 => MoveDirection.Left,
                    _ => throw new Exception(),
                });
                return true;
            }
        }
        
        private void MoveToChargingStation()
        {
            _isGoingToChargingStation = true;
            
            var fov = _mapManager.GetFieldsOfView();

            if (fov.CenterCasted is FieldType.ChargingStation)
            {
                // Todo: Recharge here.

                _isGoingToChargingStation = false;
                return;
            }
            
            // Todo: Keep moving to the charging station.
        }
    }
}