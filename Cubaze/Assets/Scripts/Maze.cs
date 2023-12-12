using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StepBFS
{
    public Cell cell;
    public int steps;

    public StepBFS() { }

    public StepBFS(Cell cell, int steps)
    {
        this.cell = cell;
        this.steps = steps;
    }
}
public class Cell
{
    public int x;
    public int y;
    public bool used;

    public Cell() { }

    public Cell(int x, int y)
    {
        this.x = x;
        this.y = y;
        used = false;
    }
}
public class Maze
{
    public int pointsEasy = 0;
    public List<List<bool>> borders;

    public int startX = 0;
    public int startY = 0;
    public int finishX = 0;
    public int finishY = 0;

    public Maze() { }

    public Maze(int pointsEasy, List<List<bool>> borders, int startX, int startY, int finishX, int finishY)
    {
        this.pointsEasy = pointsEasy;
        this.borders = borders;
        this.startX = startX;
        this.startY = startY;
        this.finishX = finishX;
        this.finishY = finishY;
    }
}
