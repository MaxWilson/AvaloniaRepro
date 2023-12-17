To repro the Avalonia bug,

1.) dotnet run
2.) In the app, click "Add Ryan" and then "Add Sarah"
3.) Click "Say something Sarah" or "Say something Ryan".

Expected: "Sarah: Blahblahblah" or "Ryan: Blahblahblah"
Actual: "Bob: Blahblahblah"

Apparently what's happening is that the original handler for Speak("Bob", "Blahblahblah") doesn't get updated even when the button text changes.