using System.Collections.Generic;
using UnityEngine;

// Odd-sized passage grid. DFS connects every cell; four portals are carved
// back to the connected interior. All solid geometry uses this same grid.
public sealed class DungeonMazeLayout
{
    public readonly int width, height;
    public readonly bool[,] open;
    public readonly Vector2Int[] portals;
    public Vector2Int reward;
    public DungeonMazeLayout(int width, int height, int seed)
    {
        this.width = Mathf.Clamp(width|1,5,21); this.height = Mathf.Clamp(height|1,5,21);
        width=this.width; height=this.height;
        open = new bool[width,height];
        var rng = new System.Random(seed); var stack = new Stack<Vector2Int>();
        var start = new Vector2Int(1,1); open[1,1]=true; stack.Push(start);
        while(stack.Count>0)
        {
            var c=stack.Peek(); var choices=new List<Vector2Int>();
            foreach(var d in DungeonLayout.Directions)
            {
                var n=c+d*2;
                if(n.x>0 && n.y>0 && n.x<width-1 && n.y<height-1 && !open[n.x,n.y]) choices.Add(n);
            }
            if(choices.Count==0) { stack.Pop(); continue; }
            var next=choices[rng.Next(choices.Count)]; var middle=(c+next)/2;
            open[middle.x,middle.y]=true; open[next.x,next.y]=true; stack.Push(next);
        }
        portals=new[]{new Vector2Int(width/2,height-1),new Vector2Int(width/2,0),new Vector2Int(width-1,height/2),new Vector2Int(0,height/2)};
        foreach(var portal in portals)
        {
            var target=new Vector2Int(Mathf.Clamp(portal.x|1,1,width-2),Mathf.Clamp(portal.y|1,1,height-2));
            var c=portal; open[c.x,c.y]=true;
            while(c.x!=target.x) { c.x+=System.Math.Sign(target.x-c.x); open[c.x,c.y]=true; }
            while(c.y!=target.y) { c.y+=System.Math.Sign(target.y-c.y); open[c.x,c.y]=true; }
        }
        // Reward is deep in the maze, preferring dead ends so a solid chest
        // cannot obstruct a through-route between doors.
        var distances=new int[width,height]; var queue=new Queue<Vector2Int>();
        for(int x=0;x<width;x++) for(int y=0;y<height;y++) distances[x,y]=-1;
        foreach(var portal in portals) { distances[portal.x,portal.y]=0; queue.Enqueue(portal); }
        while(queue.Count>0)
        {
            var c=queue.Dequeue();
            foreach(var d in DungeonLayout.Directions)
            {
                var n=c+d;
                if(IsOpen(n) && distances[n.x,n.y]<0) { distances[n.x,n.y]=distances[c.x,c.y]+1; queue.Enqueue(n); }
            }
        }
        int best=-1; reward=start;
        for(int x=1;x<width-1;x+=2) for(int y=1;y<height-1;y+=2)
        {
            var c=new Vector2Int(x,y); int neighbours=0;
            foreach(var d in DungeonLayout.Directions) if(IsOpen(c+d)) neighbours++;
            int score=distances[x,y]+(neighbours==1 ? 1000 : 0);
            if(score>best) { best=score; reward=c; }
        }
    }
    public bool IsOpen(Vector2Int c) => c.x>=0 && c.y>=0 && c.x<width && c.y<height && open[c.x,c.y];
    public Vector2 Position(Vector2Int c,float pitch) => new Vector2(c.x-(width-1)*0.5f,c.y-(height-1)*0.5f)*pitch;
}
