#!mission mis_test

metadata() {
  name Test Mission
  type main
  start_type email
  giver ashe
}

email() {
  echo Hello world.
  echo This is a test.
  echo If you are reading this, "<b>this text should be bold.</b>"
}

start() {
  PLAYER_HOME="$(playerhome)";
  FLAGFILE="/player${PLAYER_HOME}/testflag.txt";
  
  # Drop a file into the player's /home directory.
  echo "This is a test flag file" > ${FLAGFILE}
  
  # Required task: Delete the flag
  # Challenge: Ping New Cipher Medical's network
  # Hidden challenge: Ping New Cipher Today's network.
  #
  # The ampersands tell the game to post these objectives simultaneously
  # and wait for them all to clear. This is how you set the player on multiple
  # tasks at once.
  objective "Delete the flag in your home directory" deletefile ${FLAGFILE} &
  challenge "Ping New Cipher Medical\'s network" ping newciphermedical &
  hidden "Ping New Cipher Today\'s website" ping newciphertoday
  
  # Create a checkpoint to restore from for when the player deletes the file later.
  checkpoint endurance_test
  
  # Required task: Create the testflag file you just deleted.
  # This objective won't be posted until the above objective list is cleared.
  objective "Re-create the deleted flag file" writefile ${FLAGFILE}
  
  # Required task: Wait 5 minutes.
  # Hidden: DO NOT delete the flag!
  objective "Wait 1 minute without deleting the flag file" wait 1m &
  hidden "Do not delete the flag file!" --missionfail deletefile ${FLAGFILE}
}