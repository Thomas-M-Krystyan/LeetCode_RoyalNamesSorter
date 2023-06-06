internal class RoyalNames
{
    // ------------------
    // Converting numbers
    // ------------------
    internal interface INumbersConvertingService
    {
        /// <summary>
        /// Translates numbers from Roman system into Arabic system.
        /// </summary>
        /// <returns>Arabic number.</returns>
        internal int RomanToArabic(string romanNumeral);
    }

    internal sealed class NumbersConvertingService : INumbersConvertingService  // AddSingleton<T>() in IServiceCollection
    {
        private readonly Dictionary<string, int> _cachedRomanToArabicMap = new()
        {
            { "I", 1 },
            { "V", 5 },
            { "X", 10 },
            { "L", 50 }
        };

        private readonly char[] _allowedDuplicates = new[] { 'I', 'X' };

        private readonly IFeedbackService _feedbackService;

        internal NumbersConvertingService(IFeedbackService feedbackService)
        {
            _feedbackService = feedbackService;
        }

        /// <inheritdoc cref="INumbersConvertingService.RomanToArabic(string)"/>
        /// <exception cref="ArgumentException"/>
        int INumbersConvertingService.RomanToArabic(string romanNumeral)  // O(log n)
        {
            // Cached Arabic number
            if (_cachedRomanToArabicMap.TryGetValue(romanNumeral, out int arabicNumber))  // O(1)
            {
                return arabicNumber;
            }

            // New Arabic number
            arabicNumber = DetermineArabicNumber(romanNumeral);  // O(log n) with caching; O(n) without caching of results

            _cachedRomanToArabicMap.Add(romanNumeral, arabicNumber);  // O(1), memoize (caching result to not calculate it n-times)

            return arabicNumber;
        }

        // O(n) complexity where "n" is only the Roman numeral length (insignificant)
        private int DetermineArabicNumber(string romanNumeral)
        {
            char[] separateLetters = romanNumeral.ToArray();

            int finalArabicNumber = 0;

            const int DefaultCounterState = 1;
            int maxThreeOccurrencesCounter = DefaultCounterState;  // Some Roman numerals can occur up to 3 times

            for (int index = 0; index < separateLetters.Length; index++)
            {
                char currentRomanLetter = separateLetters[index];
                int currentArabicDigit = GetArabicNumber(romanNumeral, currentRomanLetter);

                // Forward lookup
                if (index + 1 < separateLetters.Length)
                {
                    char nextRomanLetter = separateLetters[index + 1];
                    int nextArabicDigit = GetArabicNumber(romanNumeral, nextRomanLetter);

                    // Add concatenable Roman letters (e.g., "III" or "XX")
                    if (currentRomanLetter == nextRomanLetter)
                    {
                        GuardAgainstOccurringMoreTimes(currentRomanLetter);
                        GuardAgainstOccurringMoreThan3Times(currentRomanLetter, maxThreeOccurrencesCounter);

                        maxThreeOccurrencesCounter++;

                        finalArabicNumber += currentArabicDigit;

                        continue;  // Prevent reseting "the rule of 3" counter (yet), keep counting if necessary
                    }
                    // Add larger preceding Roman letter (e.g., case "VI": "V" is larger than "I" => 5 + 1 => result: 6)
                    else if (currentArabicDigit > nextArabicDigit)
                    {
                        finalArabicNumber += currentArabicDigit;
                    }
                    // Substract smaller preceding Roman letter (e.g., case "IV": "I" is smaller than "V" => -1 + 5 => result: 4)
                    else
                    {
                        finalArabicNumber -= currentArabicDigit;
                    }

                    maxThreeOccurrencesCounter = DefaultCounterState;  // Reset "the rule of 3" counter
                }
                // The last Roman letter can be always added
                else
                {
                    finalArabicNumber += currentArabicDigit;
                }
            }

            return finalArabicNumber;
        }

        private int GetArabicNumber(string romanNumeral, char romanLetter)
        {
            if (!_cachedRomanToArabicMap.TryGetValue(romanLetter.ToString(), out int arabicNumber))
            {
                _feedbackService.Error(ErrorMessages.InvalidRomanNumeral, romanLetter, romanNumeral);
            }

            return arabicNumber;
        }

        private void GuardAgainstOccurringMoreTimes(char romanLetter)
        {
            if (!_allowedDuplicates.Contains(romanLetter))
            {
                _feedbackService.Error(ErrorMessages.MultipleOccurrences, romanLetter);
            }
        }

        private void GuardAgainstOccurringMoreThan3Times(char romanLetter, int counter)
        {
            if (counter == 3)
            {
                _feedbackService.Error(ErrorMessages.MoreThanThreeOccurrences, romanLetter);
            }
        }
    }

    // ----------------
    // Feedback service
    // ----------------
    internal interface IFeedbackService
    {
        internal void Error(string message, object argument1);

        internal void Error(string message, object argument1, object argument2);
    }

    internal sealed class ExceptionFeedbackService : IFeedbackService  // Can be mocked, printed to console, logged (ILogger), etc.
    {
        void IFeedbackService.Error(string message, object argument1)
        {
            throw new ArgumentException(BasicError(message, argument1));
        }

        void IFeedbackService.Error(string message, object argument1, object argument2)
        {
            throw new ArgumentException($"{BasicError(message, argument1)}, from provided \"{argument2}\"");
        }

        private static string BasicError(string message, object argument1)
        {
            return $"ERROR: {message}: \"{argument1}\"";
        }
    }

    // ----------------
    // User interaction
    // ----------------
    internal static class ErrorMessages  // An equivalent of "Resources.resx" file
    {
        internal const string InvalidRomanNumeral = "This Roman letter cannot be recognized as part of valid numeral";
        internal const string MoreThanThreeOccurrences = $"Having more than 3 occurrences of this Roman letter is not allowed";
        internal const string MultipleOccurrences = "Having more than 1 occurrence of this Roman letter is not allowed";
    }

    // ------------------
    // Sorting strategies
    // ------------------
    internal interface ISortingStrategy<T>
    {
        internal IList<T> Sort(IList<T> collection);
    }

    internal sealed class RoyalNamesSortingStrategy : ISortingStrategy<string>
    {
        private readonly INumbersConvertingService _numbersConvertingService;

        private class RoyalName
        {
            internal string FullRoyalName { get; }

            internal string Name { get; }

            internal string RomanOrdinalNumber { get; }

            internal int ArabicOrdinalNumber { get; }

            internal RoyalName(string fullRoyalName, string name, string romanNumeral, int arabicNumber)
            {
                FullRoyalName = fullRoyalName;
                Name = name;
                RomanOrdinalNumber = romanNumeral;
                ArabicOrdinalNumber = arabicNumber;
            }
        }

        internal RoyalNamesSortingStrategy(INumbersConvertingService numbersConvertingService)
        {
            _numbersConvertingService = numbersConvertingService;
        }

        IList<string> ISortingStrategy<string>.Sort(IList<string> collection)  // O(n^2) but O(n log (n)) is possible
        {
            if (collection.Any())  // O(1) in .NET Core and .NET 5+; do not use it in .NET Framework O(n)
            {
                // Parsing data
                RoyalName[] royalNames = new RoyalName[collection.Count];  // O(1)
                                                                           // Sacrificing space complexity to achieve better readability than "Tuple(string, string, string, int)"

                // O(n^2); however, O(log n) is possible after restructurizing the input data
                for (int index = 0; index < collection.Count; index++)  // O(n)
                {
                    string royalName = collection[index];  // O(1)
                    string[] nameAndRomanNumber = royalName.Split(' ');  // O(n)  => To be improved into O(1), see comments

                    // NOTE: All index-based get/set operations: O(1); cration of a new object: O(1)
                    royalNames[index] = new RoyalName(royalName, nameAndRomanNumber[0], nameAndRomanNumber[1],
                                                      _numbersConvertingService.RomanToArabic(nameAndRomanNumber[1]));  // O(log n)
                }

                // Sorting data
                return GetSortedList(royalNames);  // O(n log (n))
            }

            return Array.Empty<string>();
        }

        private static List<string> GetSortedList(RoyalName[] royalNames)  // O(n log(n))
        {
            return royalNames
                .OrderBy(royalName => $"{royalName.Name} {royalName.ArabicOrdinalNumber}")  // O(n log(n))
                .Select(royalName => $"{royalName.FullRoyalName}")  // O(n)
                .ToList();  // O(n)
        }
    }

    // --------
    // Workflow
    // --------
    private static void Main()
    {
        TextWriter textWriter = new StreamWriter(Environment.GetEnvironmentVariable("OUTPUT_PATH")!, true);

        int namesCount = Convert.ToInt32(Console.ReadLine()!.Trim());

        List<string> names = new();

        for (int i = 0; i < namesCount; i++)
        {
            string namesItem = Console.ReadLine()!;
            names.Add(namesItem);  // NOTE: Splitting "namesItem" by " " will spare this O(n) effort later and decrease complexity.
                                   // Providing data as string[2], KeyValuePair<string, string>[], RoyalNames[], (string, string)
                                   // will reduce the complexity even in the Main() method and keep the lowest possible one later
        }

        // -----------------------------------------------------------
        // Should be replaced by proper Dependency Injection mechanism
        // -----------------------------------------------------------
        IFeedbackService feedbackService = new ExceptionFeedbackService();
        INumbersConvertingService convertingService = new NumbersConvertingService(feedbackService);
        ISortingStrategy<string> sortingStrategy = new RoyalNamesSortingStrategy(convertingService);

        IList<string> result = sortingStrategy.Sort(names);
        // -----------------------------------------------------------

        textWriter.WriteLine(string.Join("\n", result));

        textWriter.Flush();
        textWriter.Close();
    }
}

// ----------------------
// Unit tests(all green)
// ----------------------
// NOTE: Recommendation: enhancing validation for Roman numerals to cover all possible edge cases e.g., invalid "IIV")

//[TestFixture]
//public class NumbersConvertingServiceTests
//{
//    private INumbersConvertingService _numbersConvertingService;
//    private ISortingStrategy<string> _sortingStrategy;

//    [OneTimeSetUp]
//    public void SetupTests()
//    {
//        ExceptionFeedbackService exceptionFeedbackService = new();

//        _numbersConvertingService = new NumbersConvertingService(exceptionFeedbackService);
//        _sortingStrategy = new RoyalNamesSortingStrategy(_numbersConvertingService);
//    }

//    // 1-10
//    [TestCase("I", 1)]
//    [TestCase("II", 2)]
//    [TestCase("III", 3)]
//    [TestCase("IV", 4)]
//    [TestCase("V", 5)]
//    [TestCase("VI", 6)]
//    [TestCase("VII", 7)]
//    [TestCase("VIII", 8)]
//    [TestCase("IX", 9)]
//    [TestCase("X", 10)]
//    // Teens
//    [TestCase("XI", 11)]
//    [TestCase("XII", 12)]
//    [TestCase("XIII", 13)]
//    [TestCase("XIV", 14)]
//    [TestCase("XV", 15)]
//    [TestCase("XVI", 16)]
//    [TestCase("XVII", 17)]
//    [TestCase("XVIII", 18)]
//    [TestCase("XIX", 19)]
//    // Tens
//    [TestCase("XX", 20)]
//    [TestCase("XXX", 30)]
//    [TestCase("XL", 40)]
//    [TestCase("L", 50)]
//    // Large numbers
//    [TestCase("XXIV", 24)]
//    [TestCase("XXIX", 29)]
//    [TestCase("XXXVIII", 38)]
//    [TestCase("XXXIX", 39)]
//    [TestCase("XLVIII", 48)]
//    [TestCase("XLIX", 49)]
//    public void Method_RomanToArabic_ForValidInput_ReturnsExpectedArabicNumber(string givenRoman, int expectedArabic)
//    {
//        // Act
//        int actualArabic = _numbersConvertingService.RomanToArabic(givenRoman);

//        // Assert
//        Assert.That(actualArabic, Is.EqualTo(expectedArabic));
//    }

//    // Illegal occurrences
//    [TestCase("IIII")]
//    [TestCase("XIIII")]
//    [TestCase("XXXX")]
//    [TestCase("VV")]
//    [TestCase("LL")]
//    // Completely wrong Roman numerals
//    [TestCase("ABC")]
//    [TestCase("Y")]
//    // Valid Roman numerals however, signalized they will not be supported
//    [TestCase("C")]
//    [TestCase("D")]
//    [TestCase("MCMXCVII")] // 1997
//    [TestCase("MMXXIII")] // 2023
//    public void Method_RomanToArabic_ForInvalidInput_ThrowsArgumentException(string givenRoman)
//    {
//        // Act & Assert
//        ArgumentException exception = Assert.Throws<ArgumentException>(() => _numbersConvertingService.RomanToArabic(givenRoman));
//    }

//    [TestCaseSource(nameof(TestRoyalNames))]
//    public void Method_Sort_ReturnsSortedRoyalNames(List<string[]> testData)
//    {
//        // Arrange
//        string[] unsortedRoyalNames = testData[0];
//        string[] expectedSortedOrder = testData[1];

//        // Act
//        IList<string> sortedRoyalNames = _sortingStrategy.Sort(unsortedRoyalNames);

//        // Assert
//        Assert.That(string.Join(", ", sortedRoyalNames), Is.EqualTo(string.Join(", ", expectedSortedOrder)));
//    }

//    private static IEnumerable<IList<string[]>> TestRoyalNames()
//    {
//        yield return new List<string[]> { new string[] { "Louis IX", "Louis VIII" }, new string[] { "Louis VIII", "Louis IX" } };
//        yield return new List<string[]> { new string[] { "Philippe I", "Philip II" }, new string[] { "Philip II", "Philippe I" } };
//    }
//}
