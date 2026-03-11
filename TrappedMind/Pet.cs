using Terminal.Gui;

namespace TrappedMind;

public static class Pet
{
    public static (string[][] Frames, Color Color) GetFrames(PetMood mood) => mood switch
    {
        PetMood.Hot => (new[]
        {
            new[] { " (>.<) ~", "  /|''|\\", "  _|  |_" },
            new[] { " (X.X) *", "  /|''|\\", "  _|  |_" },
        }, Color.Red),

        PetMood.HighCpu => (new[]
        {
            new[] { " (O.O)!", " \\|  |/", "  /  \\" },
            new[] { " (o.O)?", " /|  |\\", "  |  |" },
        }, Color.Red),

        PetMood.LowBattery => (new[]
        {
            new[] { " (-.-)z", "  /|  |", "   |  |" },
            new[] { " (-._.)z", "  /|  |", "   |  |" },
        }, Color.Blue),

        PetMood.Charging => (new[]
        {
            new[] { " (^.^)/", "  /|  |", "   |  |" },
            new[] { " (^o^)~", "  \\|  |", "   |  |" },
        }, Color.Green),

        _ => (new[]
        {
            new[] { " (o.o)", "  /|  |\\", "   |  |" },
            new[] { " (-.-)", "  /|  |\\", "   |  |" },
        }, Color.Gray),
    };
}
