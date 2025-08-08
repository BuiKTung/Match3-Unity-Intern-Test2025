using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class CollectionBoard 
{
    private int boardSizeX;
    
    private int boardSizeY;
    
    private Cell[,] m_cells;
    
    private Transform m_root;
    
    private int m_matchMin;
    
    private int m_itemCount = 0;
    
    public bool IsFull() => m_itemCount == boardSizeX * boardSizeY;
    public void OnItemAssigned(int count) => m_itemCount += count;
    public void OnItemFreed(int count) => m_itemCount -= count;

    public CollectionBoard(Transform transform, GameSettings gameSettings)
    {
        m_root = transform;
        
        m_matchMin = gameSettings.MatchesMin;

        boardSizeX = gameSettings.CollectionBoardSizeX;
        boardSizeY = gameSettings.CollectionBoardSizeY;
        
        m_cells = new Cell[boardSizeX, boardSizeY];
        
        CreateBoard();
    }

    private void CreateBoard()
    {
        Vector3 origin = new Vector3(-boardSizeX * 0.5f + 0.5f, -boardSizeY * 0.5f - 3.8f, 0f); //(x, -4.3f, 0) is good location
        GameObject prefabBG = Resources.Load<GameObject>(Constants.PREFAB_CELL_BACKGROUND);
        for (int x = 0; x < boardSizeX; x++)
        {
            for (int y = 0; y < boardSizeY; y++)
            {
                GameObject go = GameObject.Instantiate(prefabBG);
                go.transform.position = origin + new Vector3(x, y, 0f);
                go.transform.SetParent(m_root);

                Cell cell = go.GetComponent<Cell>();
                cell.Setup(x, y, BoardType.CollectionBoard);

                m_cells[x, y] = cell;
            }
        }
        
        for (int x = 0; x < boardSizeX; x++)
        {
            for (int y = 0; y < boardSizeY; y++)
            {
                //if (y + 1 < boardSizeY) m_cells[x, y].NeighbourUp = m_cells[x, y + 1]; // because y = 0
                if (x + 1 < boardSizeX) m_cells[x, y].NeighbourRight = m_cells[x + 1, y];
                //if (y > 0) m_cells[x, y].NeighbourBottom = m_cells[x, y - 1];
                if (x > 0) m_cells[x, y].NeighbourLeft = m_cells[x - 1, y];
            }
        }
    }
    
    
    
    public List<Cell> GetHorizontalMatches(Cell cell)
    {
        List<Cell> list = new List<Cell>();
        list.Add(cell);

        //check horizontal match
        Cell newcell = cell;
        while (true)
        {
            Cell neib = newcell.NeighbourRight;
            if (neib == null) break;

            if (neib.IsSameType(cell))
            {
                list.Add(neib);
                newcell = neib;
            }
            else break;
        }

        newcell = cell;
        while (true)
        {
            Cell neib = newcell.NeighbourLeft;
            if (neib == null) break;

            if (neib.IsSameType(cell))
            {
                list.Add(neib);
                newcell = neib;
            }
            else break;
        }
        
        return list;
    }
    
    internal void ShiftLeftItems()
    {
        for (int y = 0; y < boardSizeY; y++)
        {
            int shifts = 0;
            for (int x = 0; x < boardSizeX; x++)
            {
                Cell cell = m_cells[x, y];
                if (cell.IsEmpty)
                {
                    shifts++;
                    continue;
                }

                if (shifts == 0) continue;

                Cell holder = m_cells[x - shifts, y];

                Item item = cell.Item;
                cell.Free();

                holder.Assign(item);
                item.View.DOMove(holder.transform.position, 0.3f);
            }
        }
    }
   
    internal void ShiftRightItemsForInsert(int position)
    {
        for (int x = boardSizeX - 1; x > position; x--)
        {
            for (int y = 0; y < boardSizeY; y++)
            {
                Cell cell1 = m_cells[x - 1, y];
                Cell cell2 = m_cells[x, y];
                
                if(cell1.IsEmpty) continue;
                
                Item item  = cell1.Item;
                cell1.Free();
                cell2.Assign(item);
                item.View.DOMove(cell2.transform.position, 0.2f);
            }
        }        
    }
        
    public Cell FindCellToInsert(Cell insertCell)
    {
        //if (itemCount == boardSizeX) return null;
        
        for (int x = 0; x < boardSizeX; x++)
        {
            for (int y = 0; y < boardSizeY; y++)
            {
                Cell cell = m_cells[x, y];

                if (cell.IsSameType(insertCell))
                {
                        int pos = x;
                        while (pos + 1 < boardSizeX && m_cells[pos + 1, y].IsSameType(insertCell))
                            pos++;

                        if (pos + 1 < boardSizeX)
                        {
                            ShiftRightItemsForInsert(pos + 1);
                            return m_cells[pos + 1, y];
                        }
                }
            }
        }
        
        for (int x = 0; x < boardSizeX; x++)
        {
            for (int y = 0; y < boardSizeY; y++)
            {
                if (m_cells[x, y].IsEmpty)
                    return m_cells[x, y];
            }
        }
        return null;
    }
    public void MoveCell(Cell boardCell, Cell collectionBoardCell, Action callback)
    {
        Item item = boardCell.Item;
        boardCell.Free();
        collectionBoardCell.Assign(item);
        
        item.View.DOMove(collectionBoardCell.transform.position, 0.3f).OnComplete(() => callback());
    }
}
