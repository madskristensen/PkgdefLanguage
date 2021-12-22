Don't take any dependencies on Visual Studio specific types such as ITextBuffer in the parser code. 
Otherwise it will be difficult to unit test since you have to mock those types.