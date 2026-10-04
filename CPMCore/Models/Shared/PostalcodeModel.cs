using BOCore;

namespace CPMCore.Models
{
    public class PostalcodeModel
    {
        public PostalcodeModel()
        {
            _countries = new List<IdNameBO>();
        }
        private List<IdNameBO> _countries;
        public List<IdNameBO> Countries
        {
            get
            {
                return _countries;
            }
            set
            {
                _countries = value;
            }
        }
        private int _selectedCountry;
        public int CountryId
        {
            get
            {
                return _selectedCountry;
            }
            set
            {
                _selectedCountry = value;
            }
        }
        private int _selectedPostalCode;
        public int PostalCodeId
        {
            get
            {
                return _selectedPostalCode;
            }
            set
            {
                _selectedPostalCode = value;
            }
        }
    }

}
