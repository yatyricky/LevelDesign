# Level Design

使用网页实现 https://zhuanlan.zhihu.com/p/415025159

## Definition

### Stability Factor γ (稳定因子) [1]

A normalized and fast converging scalar that is calculated out from various orders of Cheeger Number.

$$\gamma_\infty=\lim_{m\to\infty}\frac{\sum_{i=1}^{m}\frac{1}{i!}\lambda_i}{\sum_{i=1}^{m}\frac{1}{i!}}=\frac{1}{e}\lim_{m\to\infty}\sum_{i=1}^{m}\frac{1}{i!}\lambda_i$$

where $\lambda_i$ is the $i$th order Cheeger Number.

### Cheeger Number

$$\lambda_k=1-\frac{n}{C_m^k}$$

# 如何使用

下载 node-calculator.html 并使用浏览器打开即可。

## Graph格式

例：

_sample.txt_

```
A>>B
B--C
C*>D
D->D
```

代表下图

![simple](simple.jpg)

### 连接符

符号|含义|英文
---|---|---
`--`|全连通路径|Undirected Path
`->`|单向路|Directed Path
`>>`|单向门|Shortcut
`*>`|机关门|Mechanism

# References

- [1]尼莫. (2021). 如何设计一张有“魂味”的地图？——论“类魂”游戏关卡的拓扑结构. https://zhuanlan.zhihu.com/p/415025159
