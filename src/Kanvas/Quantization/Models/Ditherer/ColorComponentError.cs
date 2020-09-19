namespace Kanvas.Quantization.Models.Ditherer
{
    class ColorComponentError
    {
        public int RedError { get; set; }
        public int GreenError { get; set; }
        public int BlueError { get; set; }

        public ColorComponentError()
        {
            RedError = 0;
            GreenError = 0;
            BlueError = 0;
        }
    }
}
