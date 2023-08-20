# CliUI - a simplified command line (interface) user interface

## How does it work

It maintains a dictionary with complete command lines as keys and actions/priority as values. New commands can be added as a result of a command and also commands can be removed.

When idle, the loop waits for available keypresses and then locks `Console.Out`. If a lock can be obtained, it will display the list of commands that match. A cursor can be used to select a command. By pressing enter, the command is carried out in the foreground. The command itself can start a background task.

## Programming it

One obtains an instance of the `CmdLineUi.Instance`. One can then add commands using the `Add` function before entering `CommandLoop`.

## Using it

After the program starts, it enters the `CommandLoop`. After pressing the first key, the UI presents a limited list of commands that contain the key. Further keypresses further limits the list only containing commands that have those keys in that order. Once can use the cursor keys to select the right command.

## Locking `Console.Out`

Background jobs must not interfere with a user dialog from another command. Hence, output should only happen, when the user interface is idle.

A lock might be obtained, once the UI is idle again. But then the keystrokes might have been used and nothing needs to be processed. 
