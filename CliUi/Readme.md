# CliUI - a simplified command line (interface) user interface

## How does it work

It maintains a dictionary with complete command lines as keys and actions/priority as values. 
New commands can be added as a result of a command and also commands can be removed.

When idle, the loop waits for available keypresses and then locks `Console.Out`. 
If a lock can be obtained, it will read a line from input. 
The user can enter an arbitrary sequence of characters, 
which then get matched against the internal stored list.
If no stored command matches the list, the interface will display `sorry` and wait for the next input.
If only one command matches, it will be executed immediately.
If more commands matches, the list will be displayed with the matched characters highlighted.
The user then can enter the number from that list.


## Programming it

One obtains an instance of the `CmdLineUi.Instance`. 
One can then add commands using the `Add` function before entering `CommandLoop`.

## Using it

First, the user might obtain an instance of the interface.
Then, the `Scan4Commands` can  be executed that searches the loaded assemblies for `public static void XX()` with the custom attribute `CmdLine`.
Functions with the `CmdLineAdder` can be used to add several dynamically created commands.

Then, executing `CommandLoop` will enter a loop that only the *Exit Phrase* will end. 

## Locking `Console.Out`

Background jobs must not interfere with a user dialog from another command. Hence, output should only happen, when the user interface is idle.

A lock might be obtained, once the UI is idle again. But then the keystrokes might have been used and nothing needs to be processed. 

## Paging and user responses

When listing larger content, the function `Pager` can be invoked after each line. 
When enough rows were displayed, it waits for user interaction. 
Just pressing *Enter* continues with the display. 
Everything else raises a `CmdLineInterrupt` with the list of words in `Words` and numbers in `Numbers`. 
One can specify a range of numbers `<lower end>-<upper end>`, which gets translated into a *list of integers*.

## Debugging

The public field `Debug` increases verbosity.
