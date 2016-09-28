using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace CarRentalSolution_S00165794
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        //Establish data context
        DataClasses1DataContext DB = new DataClasses1DataContext();
        public enum CarTypes { All, Large, Medium, Small }

        //Declare and initialize a list of CarExtended objects to store the cars in the car rental's pool
        ObservableCollection<CarExtended> carList = new ObservableCollection<CarExtended>();

        //Declare and initialize a list of CarExtended objects to store available cars coming from the 
        //search button click event handler
        ObservableCollection<CarExtended> carsAvailableList = new ObservableCollection<CarExtended>();

        //Declare list of CarExtended objects to store filtered list of vehicles depending on users selection from combo box (All, Large, Medium, Small)
        ObservableCollection<CarExtended> largeCarList = new ObservableCollection<CarExtended>();
        ObservableCollection<CarExtended> mediumCarList = new ObservableCollection<CarExtended>();
        ObservableCollection<CarExtended> smallCarList = new ObservableCollection<CarExtended>();


        public MainWindow()
        {
            InitializeComponent();
        }

        #region EVENT HANDLERS
        //Load Event handler for the main window
        private void CarRental_System_Loaded(object sender, RoutedEventArgs e)
        {
            //Populate the dropdown menu with the enum options
            cbxCarType.ItemsSource = Enum.GetNames(typeof(CarTypes)).ToList();

            //Create all carExtended objects from the Car table. This will allow to 
            // use new properties such as the image to be displayed, overriden and new methods.

            //Query the car table to get all the records and used to create CarExtended objects
            var carsInPool = from vehicle in DB.Cars
                             select vehicle;
            // foreach loop to create a CarExtended object from each car record in the Car table
            foreach (var vehicle in carsInPool)
            {
                CarExtended carExt = new CarExtended(vehicle.VIN, vehicle.Make, vehicle.RegNumber, vehicle.Model, vehicle.Size);
                carList.Add(carExt);
            }// End of foreach loop
        }// End of the main window loaded envent handler

        //Click Event handler for the Search button
        private void btnSearch_Click(object sender, RoutedEventArgs e)
        {
            if (AreDatesNull()) {
                //Check the dates are correct: Date is not before today's date and end date is greater or equal that start date
                if (CheckDates()) {
                    //Clear the list of available cars 
                    ClearCarsAvailableList();
                    //Clear filterd lists
                    ClearFilteredLists();

                    /*The following piece of code is ment to return all those cars availabe for a specific date range
                     The following steps are carried out:
                     A) Determine the cars unabailabe for the specified date range.
                     B) Get all the cars (via car ID or VIN) in the Booking table.
                     C) Short the list of cars with bookings by removing duplicates.
                     D) Go through the the short list of cars with bookings and check wheter each car 
                        is available or not for the specified date range.
                     E) Next, all cars from the Car table that have no booking yet must be included.
                     F) Finally, a list of CarExtended objects is created by including all the cars in the 
                        pool that actaully are available for the specified date range.
                    */


                    /*A) Determine the cars unabailabe for the specified date range.
                      Query the database to check all unavailable vehicles during the selected date range.
                      This is achieved by checking 4 posibilities: 
                      1) A car is booked before the new requested start date and returned within the requested date range. 
                      2) A car is booked withing the requested date range for a shoter or equeal period of time.
                      3) A car is booked withtin the requested date range and it will be returned after the requested end date
                      4) A car is booked for period of time that completely covers the requested date range
                    */
                    var carsUnvailable = from booking in DB.Bookings
                                        where ((booking.StartDate <= dpStartDate.SelectedDate &&
                                         (booking.EndDate >= dpStartDate.SelectedDate && booking.EndDate <= dpEndDate.SelectedDate)) ||//Posibility 1
                                         (booking.StartDate >= dpStartDate.SelectedDate && booking.EndDate <= dpEndDate.SelectedDate) ||// Posibility 2
                                         ((booking.StartDate <= dpEndDate.SelectedDate && booking.StartDate >= dpStartDate.SelectedDate) && 
                                                                                            booking.EndDate >= dpEndDate.SelectedDate) ||//Posibility 3
                                         (booking.StartDate <= dpStartDate.SelectedDate && booking.EndDate >= dpEndDate.SelectedDate))//Posibility 4
                                        select booking.VIN;

                    /*B) Get all the cars (via car ID or VIN) in the Booking table. 
                         This quesry returns all cars with bookings*/
                    var carsInBookings = from booking in DB.Bookings
                                         select booking.VIN;

                    /*C) Short the list of cars with bookings by removing duplicates.
                         The use of the Distinct method will return the Car ID of a car only once, 
                         even when the same car is booked several times. (This makes the code more efficient)
                    */
                    List<string> carsAvailable = new List<string>();
                    var inBookingsShortList = carsInBookings.ToList().Distinct();

                    /*D) Go through the the short list of cars with bookings and check wheter each car 
                         is available or not for the specified date range. If car ID in the short cars in booking list
                         is not in the unavailable cars list, that car ID(VIN) is included in a new list for cars available.*/
                    foreach (string VIN in inBookingsShortList) {
                        if (!carsUnvailable.Contains(VIN)) {
                            carsAvailable.Add(VIN);
                        }// End of if statement to  check if a VIN is in the unavailable cars list.
                    }// End of foreach loop

                    /*E.1) Finally, all cars from the Car table that have no booking yet must be included.
                          This query returns all cars with no booking in the Booking table.*/
                    var carsNotBooked = from vehicle in DB.Cars
                                        where !(carsInBookings.Contains(vehicle.VIN))
                                        select vehicle.VIN;

                    /*E.2) This is a union of the carAvailable collection and carNotBooked collection.
                           All cars not booked yet and available cars for the input dates will be put together n one list.*/
                    var fulljoin = carsAvailable.Union(carsNotBooked);

                    /*F) Finally, a list of CarExtended objects is created by including all the cars in the
                        pool that actaully are available for the specified date range.
                        This for each llop updates the available car list. Only the cars that meet the Search criteria 
                        would be included in this list.*/
                    foreach (string VIN in fulljoin)
                    {
                        UpdateCarsAvailableList(VIN);
                    }//End of foreach statement

                    //Checking and applying the filter chosen by the user (All, Large, Medium or Small cars)
                    if (this.cbxCarType.SelectedItem != null)
                    {
                        string filter = (string)this.cbxCarType.SelectedItem;
                        //The method to invoke will select the correct cars list to be displayed in the GUI
                        FilterByCarSize(filter);
                    }//End of if statement to check the combo box selection is not null
                }
                else {
                    MessageBox.Show("Error! End Date should be later than Start Date and both should not be previous to today's date.Please check dates!");
                }//End of date checking
            } else {
                MessageBox.Show("Please select dates for Start and End dates!");
            }// End of if statement that checks if start or end dates were selected
        }// End of btnSearch click event handler

        //Selection changed event handler for the available cars list box
        private void lbxAvailableCars_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //Checking the selected item is not null
            if (lbxAvailableCars.SelectedItem != null)
            {
                CarExtended carExt = (CarExtended)lbxAvailableCars.SelectedItem;
                this.tblkCarDetails.Text = BookingDetails(carExt);
                this.image.Source = ImageSelector(carExt.VIN);
            }// End not null selected item checking
        }// End of AvailableCars list box selection change event handler

        //Click event handler for the Book button
        private void btnBook_Click(object sender, RoutedEventArgs e)
        {
            //Check if selected car is not null
            if (lbxAvailableCars.SelectedItem != null) {
                CarExtended carExt = (CarExtended)lbxAvailableCars.SelectedItem;
                //Send a new booking record to the database
                Booking booking = new Booking
                {
                    ID = DB.Bookings.Count() + 1,
                    VIN = carExt.VIN,
                    StartDate = (DateTime) this.dpStartDate.SelectedDate,
                    EndDate = (DateTime) this.dpEndDate.SelectedDate                   
                };// End of creation of new Booking object
                //Send new booking to Booking table and update the database
                DB.Bookings.InsertOnSubmit(booking);
                DB.SubmitChanges();
                //Send confirmation message. Invoke the BookingDetails method which returns the correct string
                MessageBox.Show("Booking Confirmation\n\n"+BookingDetails(carExt));
                //Call method to reset screen
                ResetScreen();
            }// End of is statement taht check fi selected item is not null
        }// End of Book button click event handler
        
        #endregion
        #region HELPER METHODS
        private BitmapImage ImageSelector(string VIN)
        {
            //Define local variables to work out within the method
            BitmapImage image = new BitmapImage(new Uri("\\images\\OldCar.png", UriKind.Relative));
            //Check if VIN passed in is in the carList
            foreach (CarExtended vehicle in carList)
            {
                if (vehicle.VIN.Equals(VIN))
                {
                    image = vehicle.Image;
                }// End of if statement
            }// End of foreach statement
            return image;
        }// End of ImageSelector method

        //Method that search a specifc VIN number within the carList listing. 
        //If car does exist it will returned (the CarExtended object)
        private CarExtended FindCarByVIN(string VIN)
        {
            //Define local variables to work out within the method
            int i = 0;
            CarExtended carExt=null;
            bool carFound = false;
            //While loop to search while the VIN is not found and the list is not finished
            while (i <= carList.Count && !carFound)
            {
                //When a match is found the local CarExtended object is pointed to the CarExtended object
                //in the carList and the boolean flag to notice a match is found is set to true
                if (carList.ElementAt(i).VIN.Equals(VIN))
                {
                    carExt = carList.ElementAt(i);
                    carFound = true;
                }
                else // Otherwise, boolean flag still being set to false
                {
                    carFound = false;
                }// End of if statement to check VIN
                i++; // The counter to move to next element is increased by 1.
            }//End of while loop
            return carExt;
        }// End of FindCarByVIN method

        //This method receives the VIN parameter and if VIN does exits in the pool list
        // The car is included in the available cars listing. Then, the car is inlcuded in one
        // of the following lists: largeCarList, mediumCarList or smallCarList
        private void UpdateCarsAvailableList(string VIN) {
            CarExtended carExt = FindCarByVIN(VIN);
            if (carExt!=null) {
                carsAvailableList.Add(carExt);
                switch (carExt.Size.ToLower()) {
                    case "large":
                        largeCarList.Add(carExt);
                        break;
                    case "medium":
                        mediumCarList.Add(carExt);
                        break;
                    case "small":
                        smallCarList.Add(carExt);
                        break;
                    default:
                        break;
                }//End of switch statement
            }//End of if statement to check if car exists  
        }// End of UpdateCarsAvailableList method

        //Method to select the proper list to feed the list box item source properted, 
        //based on the filter (car size) selected by user (All, Large,Medium or Small)
        private void FilterByCarSize(string size) {
            switch (size.ToLower())
            {
                case "all":
                    this.lbxAvailableCars.ItemsSource = carsAvailableList;
                    break;
                case "large":
                    this.lbxAvailableCars.ItemsSource = largeCarList;
                    break;
                case "medium":
                    this.lbxAvailableCars.ItemsSource = mediumCarList;
                    break;
                case "small":
                    this.lbxAvailableCars.ItemsSource = smallCarList;
                    break;
            }// End of switch statement
        }// End of FilterByCarSize method

        //Method to clear the list of available cars for the input dates
        private void ClearCarsAvailableList()
        {
            carsAvailableList.Clear();
        }//End of ClearCarsAvailableList method

        //Method to clear the filtered by size lists.
        private void ClearFilteredLists() {
            largeCarList.Clear();
            mediumCarList.Clear();
            smallCarList.Clear();
        }// End of ClearFilteredLists

        //Method to check dates were picedk out
        private bool AreDatesNull() {
            bool datesNotNull = false;
            if (dpStartDate.SelectedDate != null && dpEndDate.SelectedDate !=  null) {
                datesNotNull = true;
            }//End of if statement 
            return datesNotNull;
        }// End of AreDatesNull method

        //Method to check the input dates are OK
        private bool CheckDates() {
            bool datesOK = false;
            if (dpStartDate.SelectedDate >= DateTime.Today && dpEndDate.SelectedDate >= DateTime.Today)
            {
                if (dpEndDate.SelectedDate >= dpStartDate.SelectedDate)
                {
                    datesOK = true;
                }// End of if statement that checks end date is valid
            }// End of if statement that check if start and end dates are not past days   
            return datesOK;
        }// End of CheckDates method

        //Method to show booking details
        private string BookingDetails(CarExtended vehicle) {
            string details = vehicle.CarDetials() + string.Format("Rental Date: {0:d}\nReturn Date: {1:d}",
                                                                dpStartDate.SelectedDate,dpEndDate.SelectedDate);
            return details;
        }// End of BookingDetails method

        //Method to clear up and reset GUI
        private void ResetScreen() {
            this.cbxCarType.SelectedItem = null;
            this.lbxAvailableCars.ItemsSource = null;
            this.dpStartDate.SelectedDate = null;
            this.dpEndDate.SelectedDate = null;
            this.image.Source = new BitmapImage(new Uri("\\images\\OldCar.png",UriKind.Relative));
            tblkCarDetails.Text = "";
        }
        #endregion

    }// End of Main Window class
}

