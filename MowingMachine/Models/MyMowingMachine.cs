﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using MowingMachine.Services;

namespace MowingMachine.Models
{
    public class MyMowingMachine
    {
        private readonly Queue<MowingStep> _mowingSteps = new();

        // These are going to be all the coordinates we go, to mow the grass at that coordinate.
        private readonly List<Field> _discoveredFields = new();

        private readonly List<(MowingStep, List<Field>, Offset)> _moves = new();

        // Contains information about the map
        private readonly MapManager _mapManager;

        // This field preserves the most recent field the mowing machine was on. Initially it will be the charging station.
        private FieldType _currentFieldType = FieldType.ChargingStation;
        
        private MoveDirection _currentFacingDirection = MoveDirection.Left;
        // private MoveDirection _currentDirection = MoveDirection.Left;
        private Field _currentField;

        private bool _isGoingToChargingStation;
        // private bool _isMowing;

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

            if (_mowingSteps.Any())
            {
                _currentFieldType = _mapManager.MoveMowingMachine(_mowingSteps.Dequeue(), _currentFieldType is FieldType.Grass ? FieldType.MowedLawn : _currentFieldType);

                if (!_mowingSteps.Any())
                {
                    _currentField = GetField(_mapManager.GetFieldsOfView(), new Offset(1, -1), _currentFieldType);
                    
                    // Update neighbor fields
                    _discoveredFields.ForEach(f => f.UpdateFieldNeighbor(_currentField));
                    _discoveredFields.Add(_currentField);
                }
                
                return false;
            }
            
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
        
        private static MowingStep CalculateStepExpense(MoveDirection direction, MoveDirection currentFacingDirection, FieldType currentFieldType)
        {
            var turns = new Queue<MoveDirection>();
            
            if (currentFacingDirection != direction)
                turns = CalculateTurn(currentFacingDirection, direction, turns);

            return new MowingStep(turns, direction, currentFieldType);
        }
        
        private Field Move(MowingStep step, Offset newOffset, FieldType? updatePrevFieldType = null)
        {
            // if (newOffset.CompareTo(new Offset(1, 3)))
            // {
            //     
            // }
            
            _currentFieldType = _mapManager.MoveMowingMachine(step, updatePrevFieldType ?? _currentFieldType);

            var type = updatePrevFieldType ?? _currentFieldType;
            
            if (_discoveredFields.Any() && type == FieldType.ChargingStation)
                // Unsure about setting mowed lawn here.
                return GetField(_mapManager.GetFieldsOfView(), newOffset, FieldType.MowedLawn);
         
            return GetField(_mapManager.GetFieldsOfView(), newOffset, type);
        }

        private static Queue<MoveDirection> CalculateTurn(MoveDirection direction, MoveDirection finalDirection, Queue<MoveDirection> moves)
        {
            if (direction == finalDirection)
                return moves;

            if (direction.InvertDirection() == finalDirection)
            {
                switch (direction)
                {
                    case MoveDirection.Top:
                    case MoveDirection.Bottom:
                        moves.Enqueue(MoveDirection.Left);
                        break;
                    case MoveDirection.Right:
                    case MoveDirection.Left:
                        moves.Enqueue(MoveDirection.Top);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(direction), direction, null);
                }
            }
            
            moves.Enqueue(finalDirection);
            return moves;
        }

        private void NoteNextMove(MowingStep step, Offset offset)
        {
            for (var i = 0; i < _moves.Count; i++)
            {
                var (_, offsets, relatedOffset) = _moves[i];
                
                var neighbors = _discoveredFields.Single(f => f.Offset.CompareTo(relatedOffset)).NeighborFields;

                if (neighbors is null)
                    continue;
                
                offsets.Clear();
                offsets.AddRange(neighbors.Where(nf => !nf.IsVisited && nf.CanBeWalkedOn()));
            }
            
            var unvisitedFields = _discoveredFields.Single(f => f.Offset.CompareTo(offset))
                .NeighborFields!.Where(nf => !nf.IsVisited && nf.CanBeWalkedOn())
                .ToList();
            
            _moves.Add((step, unvisitedFields, offset));
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
            steps.ForEach(_mowingSteps.Enqueue);
            
            var nextStep = _mowingSteps.Dequeue();
            
            // Unsure
            _currentFacingDirection = nextStep.MoveDirection;
            // _currentFacingDirection = steps.Last().MoveDirection;
            
            if (_currentField.Offset.CompareTo(new Offset(0,0)))
                Console.WriteLine("test");

            var oldOffset = _discoveredFields.Last().Offset;
            var newOffset = oldOffset.Add(new Offset(nextStep.MoveDirection));
            
            _currentField = Move(nextStep, newOffset,
                _currentFieldType is FieldType.Grass ? FieldType.MowedLawn : _currentFieldType);
            
            _currentField.IsVisited = true;

            if (_discoveredFields.Any(f => f.Offset.CompareTo(_currentField.Offset)))
            {
                _discoveredFields.Move(_discoveredFields.First(f => f.Offset.CompareTo(_currentField.Offset)), _discoveredFields.Count);
                NoteNextMove(nextStep, newOffset);
                return;
            }
            
            // Update neighbor fields
            _discoveredFields.ForEach(f => f.UpdateFieldNeighbor(_currentField));
            _discoveredFields.Add(_currentField);
            
            NoteNextMove(nextStep, newOffset);
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
                    var (prevFieldStep, neighbors, offset) = _moves[i];

                    if (!neighbors.Any())
                    {
                        var step = CalculateStepExpense(prevFieldStep.MoveDirection.InvertDirection(), currentDirection, currentlyStandFieldType);
                        currentlyStandFieldType = prevFieldStep.FieldType;
                        steps.Add(step);

                        currentDirection = step.MoveDirection;
                    }
                    
                    
                    if (neighbors.Any())
                    {
                        var calculatedOffset = neighbors
                            .Select(nf => nf.Offset.Subtract(offset)).First();
                        
                        var finalDirection = MowingMachineService.TranslateOffsetToDirection(calculatedOffset);
                        
                        var stepFinal = CalculateStepExpense(finalDirection, currentDirection, prevFieldStep.FieldType);
                        steps.Add(stepFinal);
                        break;
                    }
                }

                return steps;
            }
            
            var nextStep = CalculateStepExpense(direction, _currentFacingDirection, _currentFieldType);

            return new List<MowingStep> { nextStep };

            bool GetNextNeighborField(out (Field, MoveDirection) result)
            {
                var fieldIndex = _currentField.NeighborFields?.FindIndex(f => !f.IsVisited && f.CanBeWalkedOn());
                result = (null, MoveDirection.Bottom);
                
                if (!fieldIndex.HasValue)
                    return false;
                
                if (fieldIndex.Value == -1)
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
            // We can use dijkstra with the help of our discovered list maybe?
        }
    }
}