# Sudoku-Solver

A Unity-based Sudoku app that can solve, generate, and (hopefully eventually) grade puzzles. It’s a remake of my old “Sudoku-Bot,” which… let’s just say aged like milk (spaghetti code, bad UI, no real strategy). This version has a clean class architecture (Board, Square, SquareGroup), a heuristic-driven solver that goes beyond “Sole Candidate” into real Sudoku techniques, and a UI that doesn’t look like programmer art. Basically: less dinner, more polish.

## The Goal

1. Write a Sudoku bot that can solve most, if not all, Sudoku puzzles within a reasonable time frame.
2. Make a fun Sudoku app.
3. Make a generator that can create and grade a new Sudoku board.

## Building the Base

### Sudoku-Bot

Back in 2023 I decided to try a Sudoku puzzle, which I had played before but never with much interest. However, that time I quickly realized it was a logic puzzle (duh) and that it scratched a similar itch to writing good code. So obviously I decided it would be a fun and easy hobby project.

The result of the project was reasonably successful. I did no research, but still managed to get most of the way there. The algorithm simply added the notes for each cell (the notes being the cell's possible numbers) and then, if there was only a single number, it would fill it in. Repeat until solved. No heuristics. No backtracking.

While I'm sure I could have figured out more (or done some damn research), the passion for the project faded and I didn't push much further. Jump forward two years and I'm out of a job, refurbishing my portfolio with newer projects. I knew I had worked on a Sudoku solver and figured, why not? It's interesting enough to showcase. I blew off the metaphorical dust and booted it up... and yikes. Yes, the solver ran and did solve puzzles, but the visuals didn’t receive the same attention. Then I checked the code, and it was spaghetti. Obviously I should drop everything and rewrite this project.

### Sudoku-Solver (Sudoku-Bot-2?)

While not listed among the goals, the unofficial aim of the project was to not look like crap. That meant a codebase that didn't look like dinner. It's either a testament to how much better I've become as a dev, or how little effort went into the original project, because I wrote a solid base of classes in a couple of hours: `Board`, `SquareGroup`, and `Square`.

#### Square

The squares are what the user and solver will both interact with. Its goals are:

* Visualize its value (0 is empty, 1 through the board size are visual).
* Be interactable so its value can be changed.
* Support notes.

Originally the `Square` class was just a single `MonoBehaviour` object, but as the solver evolved it became necessary to create the `ISquare` interface. There are two variants of `Square`, but they both serve the same purposes.

When changed, a `Square` invokes the event `OnChanged<int, int>`, which sends the previous value and the new value. The version of `Square` that a user interacts with triggers this event with the `OnClick` event of its button, but both version trigger it when the number changes.

`ISquare` includes the `Notepad` class, which tracks notes as a `bool[]`. The "main" square extends the class by adding an array of `TMP_Text` objects. Each square's notepad automatically toggles the relevant text object when triggered.

#### SquareGroup

The `SquareGroup` class is a parentless class. Its goals are:

* Hold a list of its member squares.
* Maintain a `bool Valid` that indicates whether its members conform to the rules of Sudoku.

When a square is added via the `PushSquare` method, the group subscribes to its `OnChanged` event. Every time a square's value updates, the group updates its `Valid` bool. The group also subscribes to the square's notepad events.

In an effort to optimize the system, each group also maintains two `int[]` arrays. One tracks the count of instances each number appears in the group (if two squares both have a 3, the 3rd position in the array will be 2), and the other tracks the squares' notes. To check validity the group simply has to verify the numbers array conforms to the rules: the counts for each number must be less than 2.

The group holds references to its members as `ISquare` types, so `SquareGroup` has its own event: `OnValidityChanged`. The "main" squares subscribe to this event and update their colours accordingly.

#### Board

The `Board` class' goals are:

* Create the instances of groups and squares.
* Put them in the UI.
* Hold references to everything.
* Validate the board when the game is solved.

One aspect of the original project I really disliked was how it handled game elements in the hierarchy and scene. While I'm sure the idea employed could be cleaned up and reused, I much prefer using absolute positions - basically the Unity UI anchor system. When used properly, it allows the UI elements to be much more flexible.

When initialized, the board takes a 2D array of integers. It checks the dimensions of the board and determines the height and width of the quadrants.

```c#
float sqrtSize = Mathf.Sqrt(_boardSize);
squareCount = new Vector2Int(
   Mathf.CeilToInt(sqrtSize),
   Mathf.FloorToInt(sqrtSize)
);
```

This means it can handle any board size. Using this it loops through all the provided numbers and assigns them to squares. It also assigns each square to three groups: the rows, columns, and quadrants. Finally it adds each new square to its own list.

When the game is ready to be validated, the board simply checks the validity of each `SquareGroup`. If there are any invalid groups, the board is also invalid.

Like `Square` and `ISquare`, `Board` also has its own interface called `IBoard`.

##### IBoard.State

For myriad reasons, it became necessary to create a way for the board to save and load state. Both implementations of `IBoard` can be saved and loaded. To convert from one to the other, they both convert to `IBoard.State` and the other loads it.

The class was also written so the game can load and save to disk.

## Building the Solver

### The Core Logic

Creating the first version of the new solver wasn't difficult. I reused the same strategy as before (if one note, apply it, repeat), but I knew I would need to add a backtracking element once it ran out of obvious moves. In the event of a square having no notes (no number that conforms to the rules), the method would return immediately.

My first implementation was pretty basic: loop through all the squares with two notes and see what sticks. It would create a board state so it could revert if the branch was a dead end, then recursively call itself. If the branch was indeed dead, it would assign the next note or move to the next number and repeat. Failing that, it would increase the "notes bar" from two to three and repeat — until the maximum notes a square could have was hit. Then it would exit.

With infinite time and patience, that version of the solver could theoretically work. But I knew I could do better, so I finally had the idea to add a heuristic. Before fully committing to assigning a note to a square, the solver would get its score. The score is the number of squares with a single note the change generates. Square–note pairs with the highest score would be checked first. The solver immediately improved drastically.

I still wasn't satisfied, however, because there were some boards that refused to be solved. I was also experimenting with using the solver to generate new boards. While I was happy that it was as simple as running the solver against a board with a single filled square, I found that it would frequently fail (within a reasonable time).

So I finally did some research and discovered just how ignorant I had been on Sudoku strategy. But I was also pretty far down the right track. [One paper](https://medium.com/@davidcarmel/solving-sudoku-by-heuristic-search-b0c2b2c5346e) finally led me to a [site describing many techniques](https://www.kristanix.com/sudokuepic/sudoku-solving-techniques.php), and I discovered my only strategy had been the "Sole Candidate" technique - literally the most basic of all the techniques (a fact that did not surprise me). So I moved on to the next strategy, the Unique Candidate.

The Unique Candidate is simply checking if a square possesses the only instance of a note within one of its groups. For example, take a square that could be 1, 3, or 5. If all other squares in one of its groups can be, or are, every other number except for 3, then you can safely assign 3 to the square.

Implementing this strategy immediately improved my solver significantly. Most puzzles are solved within a second, and most of the rest solve within 10. The generator still fails sometimes, but it's 1 in 10 rather than 1 in 4. And that's the point at which I think I'm satisfied - for the time being.

### Visualizing The Solver

The original solver and the first version of this one were both written as `Coroutines`. I think watching an algorithm work in real time is fascinating, and I wanted that feature for my project. But as I started finding that the solver would spend forever working a puzzle I decided that the coroutine wasn't fast enough.

Obviously I could just write a normal recursive method and let it go, but locking up your system is a great way to lose time and progress. So the method would have to be `async`. This decision led to the rewrite that resulted in the `IBoard` and `ISquare` interfaces. One version of their implementations for visualizing the algorithm, and the other for the `async` solver.

## Building the UI

My next task is to wrap my functional but ugly programmer UI into something presentable.
