# color-id-game


# setup
Before playing the game, you need to check three variables. Go to the Game scene, the "ui text" GameObject, and look at the component Game Engine (Script). The public variables "Type", "Goal Percentage", and "Initial Clock" should be set to "grayscale" (if using Unity Editor) or "rgb" (otherwise), a decimal between 0 and 1, and some large integer (eg >300) respectively. 


# game play
In the top center of the screen, the name of a color will be displayed. The list of possible colors is hard-coded in GameEngine.cs, only because it would take forever to make them be input throught the Unity editor. The player needs to move the camera until the percentage of pixels that match the specified color is greater than the percentage specified in the "Goal Percentage" variable in the Unity Editor. When the player succeeds, they gain a point and a new color is selected. The same color will never be chosen twice in a row. If the timer runs out, the player doesn't gain a point and a new color is selected. For the new round, the timer is set to however long the player spent on the previous round.
