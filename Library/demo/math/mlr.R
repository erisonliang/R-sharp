let a <- [1,2,3,4,5];
let b <- [2,4,6,8,10];
let c <- [1,1,1,1,1];
let mydata <- data.frame(a, b,c);

print(lm(b ~ a + c, mydata));