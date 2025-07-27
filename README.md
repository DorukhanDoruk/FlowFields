# FlowFields

Hello, unfortunately, the idea of documenting my decisions and the experience I gained came to me in the middle of my first FlowFields project.

### Dijkstra-like Approach
Up to this point, I hadn't used BFS and was working with only 50 entities on a 100x100 grid. The performance was quite satisfactory, and my GameLoop ran roughly as follows:
 * First, I would input a target into my system, set the cost of the cell containing this target to 0, and add it to the control list.
 * I would enter a loop until the control list was empty, processing each element as follows:
   * The cell with the lowest cost (A) is taken.
   * Its 8 neighbors (B) are checked, and the cost of moving from cell B to cell A is calculated.
   * If this calculated cost is lower than cell B's previously held cost, we update it and add cell B back to the control list.
While this might seem perfectly logical so far, unfortunately, this situation creates a very large loop for us and significantly increases the cost. Especially after increasing the grid size to 1000x1000 and the number of entities to 500, when there is a moving target, we can't get more than a few FPS. This problem doesn't occur with static targets; relatively, around 60 FPS can be easily achieved.
### (BFS) Breadth-First Search Logic
Above, I talked about our loop and why it was costly for us. Now, let's move on to BFS; we can fundamentally state these two sentences:
 * When a cell is visited, the shortest path to that cell has already been found.
 * Therefore, a cell is never, ever added to the queue more than once.
Based on this, the GameLoop we mentioned above should transform into this:
 * A target is entered into the system, and the cost of the cell containing this target becomes 0. (I wanted to enter an appropriate max value for the costs of other cells, such as ushort.MaxValue, as the grid is now 1000x1000 in size).
 * We start a loop again until the control list is empty, and we first process the cell where our target is located.
   * Let's call the first cell A, and we look at all its neighbors, let these neighbors be B.
   * If neighbor B's cost is still ushort.MaxValue (meaning it hasn't been visited before):
     * Calculate B's cost.
     * Set B's direction towards A.
     * Add B to the queue for the first and last time.
   * If this cost is not ushort.MaxValue (meaning it has been visited before), do not touch it and continue without doing anything.
Even with such a small change, our loop, which previously performed 10 million iterations, will now roughly perform 1 million iterations.
However, I still couldn't achieve the performance I wanted with this; when there is a moving target, the FPS continues to drop dramatically. It's better than before, but not enough.
Here I found an opportunity to rethink again:
 * **What are we doing?**: When the target moves, we run a BFS algorithm that traverses and fills all 1 million cells starting from the target.
 * **Why is it inefficient?**: Does an entity at one end of the grid truly need the ultra-detailed, calculated cost of all 999,999 cells in between to reach the target at the other end of the map? No, of course not; knowing just the general direction would be quite sufficient, but we are taking an ultra-detailed MRI of the entire grid every time. :D
### (HFF) Hierarchical Flow Fields
This is where HFF comes into play; I believe there's a better way to solve a problem of this scale.
The basic philosophy is: "Look at the grid from afar, create a general route. Focus on the details when you get closer."
I think we can do this in two steps:
### Macro Graph
 * First, I will divide our existing and massive grid into 50x50 pieces (we can call them clusters). This way, instead of 1 million cells, we will have 20x20 = 400 pieces to manage.
 * Only once at the beginning of the game, we will calculate how to travel between these pieces. For example, how to go from piece A to piece B, from piece B to piece C? This will be our route.
### Micro Graph
 * When the target moves, 1 million cells will no longer be calculated. Instead, a small and cheap local Flow Field will be created only for the cluster where the target is located, leading towards the exits of this cluster.
 * When an entity wants to move, it will first ask the macro graph, "Which neighboring cluster should I go to to reach the target?" and let's say it gets the answer "The cluster to the East."
 * Then, it will look at the local FlowField of the cluster it is in and follow the path leading to the "East" side of this cluster.
The amount of performance I plan to gain here is that perhaps only 10k cells will be calculated each time instead of 1 million. When the target moves, the remaining 399 out of 400 clusters will not need to recalculate.
Generally, I am following this path:
 * **ClusterPortal**: will represent the transition points between two clusters.
 * **GridCluster**: will represent each piece when we divide our large grid into pieces.
 * **ClusterGraph**: will manage all pieces and the portals between them.

<img width="968" height="376" alt="IMG_0035" src="https://github.com/user-attachments/assets/138b6cf7-53bd-4341-ac80-184e02ca0694" />
And the result is this: we are getting a result hovering around 160 FPS on a 1000x1000 grid with 500 entities. Entities can sometimes follow a strange path when transitioning between grids, for example:

<img width="286" height="191" alt="IMG_0036" src="https://github.com/user-attachments/assets/acbc3a53-b39d-4cb9-a2f0-dfdf675c3515" />
Here, an entity wanting to go from point X to point Y, since the shortest transition from cluster A to cluster C is its top-right corner, it proceeds to the right edge or directly to the top-right corner of cluster A, no matter where it is within cluster A. The phrase "No matter where it is!" I think, accurately describes how some illogical transitions can occur here.
I have finally resolved some of the issues I mentioned above.

<img width="1396" height="122" alt="IMG_0037" src="https://github.com/user-attachments/assets/e1287959-3a3b-4fd5-b416-7fe7b23dd00f" />
We get a result like the one above. In a deep profile, when the target remains stationary, it can go up to 160 FPS, but when the target changes clusters, the situation above occurs, and the FPS drops to the 60 band. I believe I have made all possible optimizations. The best thing left to do would be to calculate HCost more consistently and accurately to visit fewer nodes. Although MinHeap.Add and MinHeap.Remove seem costly, the real cost is that these two methods are called too frequently, as I believe both methods are as optimized as possible.
