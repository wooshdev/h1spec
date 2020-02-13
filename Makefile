bin/h1spec.exe: src/*
	@mkdir -p bin
	mcs -out:$@ $^