using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace CarRentalSolution_S00165794
{
    class CarExtended : Car
    {
        //Additional properties for the new CarExtended class
        public BitmapImage Image { get; set; }

        //Constructor to create new CarExtended objects
        #region CONSTRUCTOR
        public CarExtended(string VIN,string make, string regNum, string model, string size) {
            this.VIN = VIN;
            this.Make = make;
            this.RegNumber = regNum;
            this.Model = model;
            this.Size = size;
            // Depending on the model the path to select the proper image is selected
            // The privae method defined to select the correct images is invoked and the car model passed in
            this.Image = ImageSelection(this.Model);
        }
        #endregion

        //New methods and overrride ToString method
        #region METHODS
        public override string ToString()
        {
            return string.Format("{0} - {1}", this.Make, this.Model); ;
        }

        /*This method allows us to display a CarExtended date in the applicatin in the format show below:
            ID: WVWZZZ12347569874
            Model: Volkswagen
            Model: Jetta
            Registration#: 161LK123
        */
        public string CarDetials()
        {
            return string.Format("ID: {0}\nMake: {1}\nModel: {2}\nRegistration No: {3}\n", 
                                 this.VIN, this.Make, this.Model, this.RegNumber);
        }

        //This private method is used to select the porper image for the CarExtended object
        // the car model is passed in and a BitmapImage object is returned
        private BitmapImage ImageSelection(string model) {
            string path;
            switch (model.ToLower()) {
                case "passat": path = "\\images\\Passat.png";
                    break;
                case "corolla": path = "\\images\\Corolla.jpg";
                    break;
                case "rapid": path = "\\images\\Rapid.png";
                    break;
                case "fabia": path = "\\images\\Fabia.png";
                    break;
                case "yaris": path = "\\images\\Yaris.jpg";
                    break;
                case "e220": path = "\\images\\E220.png";
                    break;
                case "superb" : path = "\\images\\Superb.png";
                    break;
                case "b180": path = "\\images\\B180.png";
                    break;
                case "a6": path = "\\images\\A6.png";
                    break;
                case "focus": path = "\\images\\Focus.png";
                    break;
                case "aygo": path = "\\images\\Aygo.png";
                    break;
                case "a1": path = "\\images\\A1.png";
                    break;
                case "1 series": path = "\\images\\1Series.png";
                    break;
                case "jetta": path = "\\images\\Jetta.png";
                    break;
                case "clio": path = "\\images\\Clio.png";
                    break;
                default: path = "\\images\\OldCar.png";
                    break;
            }// End of the switch statement 
            return new BitmapImage(new Uri(path,UriKind.Relative));
        }// End of ImageSelector method


        #endregion
    }
}
