using WordsFinderLib;

WordsFinder wordsFinder = new WordsFinder();
wordsFinder.AddWordToForbiddenWords("123456");
wordsFinder.AddWordToForbiddenWords("мир");
wordsFinder.AddWordToForbiddenWords("33355");
wordsFinder.CommonSearchCopyUpdateMethod("C:\\KMPlayer");
//wordsFinder.CommonSearchCopyUpdateMethod();
